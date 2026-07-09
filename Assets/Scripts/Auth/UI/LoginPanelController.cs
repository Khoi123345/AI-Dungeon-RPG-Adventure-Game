using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// LoginPanelController — Gắn vào LoginPanel GameObject trong scene Login.
///
/// Kết nối với:
///   - InputField-username  → inputUsername
///   - InputField-password  → inputPassword
///   - btn_Login            → btnLogin
///   - btn_Cancel           → btnCancel
///   - Text lỗi             → txtError (tùy chọn, nếu có)
///
/// Cách setup trong Unity Inspector:
///   1. Gắn script này vào LoginPanel GameObject
///   2. Kéo thả InputFields và Buttons vào các slot tương ứng
///   3. btnLogin sẽ tự động bắt sự kiện OnLoginClicked
/// </summary>
public class LoginPanelController : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField inputUsername;
    [SerializeField] private TMP_InputField inputPassword;

    [Header("Buttons")]
    [SerializeField] private Button btnLogin;
    [SerializeField] private Button btnCancel;
    [SerializeField] private Button btnGoToRegister; // nút chuyển sang Register

    [Header("UI Feedback")]
    [Tooltip("Text hiển thị lỗi inline (không dùng popup)")]
    [SerializeField] private TMP_Text txtError;

    [Tooltip("Loading overlay hoặc spinner khi đang gọi API")]
    [SerializeField] private GameObject loadingIndicator;

    [Header("Scene Navigation")]
    [SerializeField] private string mainMenuScene   = "DemoMenu";
    [SerializeField] private string registerScene   = "Register";
    [SerializeField] private string welcomeScene    = "Welcome";

    // ── State ─────────────────────────────────────────────────────
    private bool _isLoading;

    // ══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════

    private void Awake()
    {
        AuthManager.EnsureInstance();
        ApiClient.EnsureInstance();
    }

    private void Start()
    {
        // Nếu đã đăng nhập (auto-login), chuyển thẳng sang MainMenu
        if (AuthManager.Instance.IsLoggedIn)
        {
            SceneManager.LoadScene(mainMenuScene);
            return;
        }

        SetupButtons();
        SetupInputListeners();
        HideError();
        SetLoading(false);

        // Focus vào username field
        if (inputUsername != null)
            inputUsername.Select();
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnLoginSuccess -= HandleLoginSuccess;
            AuthManager.Instance.OnAuthFailed   -= HandleAuthFailed;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // SETUP
    // ══════════════════════════════════════════════════════════════

    private void SetupButtons()
    {
        if (btnLogin != null)
        {
            btnLogin.onClick.RemoveAllListeners();
            btnLogin.onClick.AddListener(OnLoginClicked);
        }

        if (btnCancel != null)
        {
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(OnCancelClicked);
        }

        if (btnGoToRegister != null)
        {
            btnGoToRegister.onClick.RemoveAllListeners();
            btnGoToRegister.onClick.AddListener(() => SceneManager.LoadScene(registerScene));
        }

        AuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
        AuthManager.Instance.OnAuthFailed   += HandleAuthFailed;
    }

    private void SetupInputListeners()
    {
        // Submit khi nhấn Enter trong password field
        if (inputPassword != null)
            inputPassword.onSubmit.AddListener(_ => OnLoginClicked());

        // Clear error khi user bắt đầu gõ lại
        if (inputUsername != null)
            inputUsername.onValueChanged.AddListener(_ => HideError());
        if (inputPassword != null)
            inputPassword.onValueChanged.AddListener(_ => HideError());
    }

    // ══════════════════════════════════════════════════════════════
    // BUTTON HANDLERS
    // ══════════════════════════════════════════════════════════════

    private void OnLoginClicked()
    {
        if (_isLoading) return;

        string username = inputUsername?.text?.Trim() ?? "";
        string password = inputPassword?.text ?? "";

        // Client-side validation
        if (string.IsNullOrEmpty(username))
        {
            ShowError("Vui lòng nhập username.");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng nhập mật khẩu.");
            return;
        }
        if (password.Length < 6)
        {
            ShowError("Mật khẩu phải có ít nhất 6 ký tự.");
            return;
        }

        _ = DoLoginAsync(username, password);
    }

    private void OnCancelClicked()
    {
        SceneManager.LoadScene(welcomeScene);
    }

    // ══════════════════════════════════════════════════════════════
    // AUTH FLOW
    // ══════════════════════════════════════════════════════════════

    private async Task DoLoginAsync(string username, string password)
    {
        SetLoading(true);
        HideError();

        await AuthManager.Instance.LoginAsync(username, password);

        SetLoading(false);
    }

    private void HandleLoginSuccess(GameShared.Models.User user)
    {
        Debug.Log($"[LoginPanel] Login success: {user?.displayName}");
        SceneManager.LoadScene(mainMenuScene);
    }

    private void HandleAuthFailed(AuthResult result)
    {
        SetLoading(false);
        ShowError(result.errorMessage ?? "Đăng nhập thất bại. Vui lòng thử lại.");
    }

    // ══════════════════════════════════════════════════════════════
    // UI HELPERS
    // ══════════════════════════════════════════════════════════════

    private void SetLoading(bool loading)
    {
        _isLoading = loading;

        if (btnLogin != null)    btnLogin.interactable = !loading;
        if (btnCancel != null)   btnCancel.interactable = !loading;
        if (loadingIndicator != null) loadingIndicator.SetActive(loading);

        // Dim input fields during loading
        if (inputUsername != null) inputUsername.interactable = !loading;
        if (inputPassword != null) inputPassword.interactable = !loading;
    }

    private void ShowError(string message)
    {
        if (txtError == null) { Debug.LogWarning($"[LoginPanel] Error: {message}"); return; }
        txtError.text = message;
        txtError.gameObject.SetActive(true);
    }

    private void HideError()
    {
        if (txtError != null) txtError.gameObject.SetActive(false);
    }
}
