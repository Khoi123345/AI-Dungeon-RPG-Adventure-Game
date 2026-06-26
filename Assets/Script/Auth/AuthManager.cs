using System;
using System.Threading.Tasks;
using GameShared.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// AuthManager — Singleton MonoBehaviour điều phối toàn bộ Auth flow.
///
/// Trách nhiệm:
///   1. Chọn MockAuthService hay RealAuthService dựa vào GameConfigSO.useMockMode
///   2. Auto-restore session khi game khởi động
///   3. Expose API đơn giản cho UI Controllers: Login, Register, Logout
///   4. Cập nhật GameProgressService.CurrentUser sau login thành công
///   5. Bắn events để UI phản ứng (OnLoginSuccess, OnLoginFailed...)
///
/// Cách dùng trên UI:
///   await AuthManager.Instance.LoginAsync("demo", "password123");
/// </summary>
public class AuthManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────
    private static AuthManager _instance;
    public  static AuthManager Instance
    {
        get
        {
            if (_instance == null) EnsureInstance();
            return _instance;
        }
    }

    // ── Events ────────────────────────────────────────────────────
    /// <summary>Bắn khi login/register thành công.</summary>
    public event Action<User>       OnLoginSuccess;

    /// <summary>Bắn khi login/register thất bại.</summary>
    public event Action<AuthResult> OnAuthFailed;

    /// <summary>Bắn khi logout xong.</summary>
    public event Action             OnLogout;

    /// <summary>Bắn khi cần xác nhận OTP email (Plan B — Cognito).</summary>
    public event Action<string>     OnConfirmationRequired; // username

    // ── State ─────────────────────────────────────────────────────
    private IUnityAuthService _authService;

    public bool   IsLoggedIn   => _authService?.IsLoggedIn ?? false;
    public string CurrentToken => _authService?.CurrentToken;
    public User   CurrentUser  => _authService?.CurrentUser;

    // ── Scene Names ───────────────────────────────────────────────
    [Header("Scene Navigation")]
    [Tooltip("Tên scene chính sau khi đăng nhập thành công")]
    [SerializeField] private string mainMenuScene = "DemoMenu";

    [Tooltip("Tên scene Login")]
    [SerializeField] private string loginScene = "Login";

    // ══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════

    public static AuthManager EnsureInstance()
    {
        if (_instance != null) return _instance;
        var go = new GameObject(nameof(AuthManager));
        _instance = go.AddComponent<AuthManager>();
        DontDestroyOnLoad(go);
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeService();
    }

    private async void Start()
    {
        await TryAutoLoginAsync();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    // ══════════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Đăng nhập. Kết quả được bắn qua OnLoginSuccess / OnAuthFailed.
    /// </summary>
    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        var result = await _authService.LoginAsync(username, password);
        HandleAuthResult(result);
        return result;
    }

    /// <summary>
    /// Đăng ký. Kết quả được bắn qua OnLoginSuccess / OnAuthFailed / OnConfirmationRequired.
    /// </summary>
    public async Task<AuthResult> RegisterAsync(string username, string email, string password, string confirmPassword)
    {
        var result = await _authService.RegisterAsync(username, email, password, confirmPassword);

        if (result.requiresConfirmation)
        {
            OnConfirmationRequired?.Invoke(username);
            return result;
        }

        HandleAuthResult(result);
        return result;
    }

    /// <summary>Xác nhận OTP (Plan B Cognito). Plan A bỏ qua.</summary>
    public async Task<AuthResult> ConfirmSignUpAsync(string username, string code)
    {
        var result = await _authService.ConfirmSignUpAsync(username, code);
        if (result.success)
            Debug.Log("[AuthManager] Account confirmed. User can now login.");
        else
            OnAuthFailed?.Invoke(result);
        return result;
    }

    /// <summary>Đăng xuất và về màn hình Login.</summary>
    public async Task LogoutAsync()
    {
        await _authService.LogoutAsync();
        GameProgressService.Instance?.ClearUser();
        OnLogout?.Invoke();

        if (SceneManager.GetActiveScene().name != loginScene)
            SceneManager.LoadScene(loginScene);
    }

    // ══════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════

    private void InitializeService()
    {
        bool useMock = GameConfigSO.Instance.useMockMode;

        if (useMock)
        {
            _authService = new MockAuthService();
            Debug.Log("[AuthManager] Using MockAuthService (offline mode).");
        }
        else
        {
            ApiClient.EnsureInstance();
            ApiClient.Instance.SetBaseUrl(GameConfigSO.Instance.apiBaseUrl);
            _authService = new RealAuthService();
            Debug.Log("[AuthManager] Using RealAuthService (online mode).");
        }
    }

    private async Task TryAutoLoginAsync()
    {
        bool restored = await _authService.TryRestoreSessionAsync();
        if (restored && _authService.CurrentUser != null)
        {
            ApplyUserToGameProgress(_authService.CurrentUser);
            OnLoginSuccess?.Invoke(_authService.CurrentUser);
            Debug.Log("[AuthManager] Auto-login success.");
        }
        else
        {
            Debug.Log("[AuthManager] No valid session found. User must login.");
        }
    }

    private void HandleAuthResult(AuthResult result)
    {
        if (result.success)
        {
            ApplyUserToGameProgress(_authService.CurrentUser);
            OnLoginSuccess?.Invoke(_authService.CurrentUser);
        }
        else
        {
            OnAuthFailed?.Invoke(result);
        }
    }

    private void ApplyUserToGameProgress(User user)
    {
        if (user == null) return;
        GameProgressService.EnsureInstance();
        GameProgressService.Instance.SetCurrentUser(user);
        Debug.Log($"[AuthManager] GameProgressService updated with user: {user.displayName}");
    }
}
