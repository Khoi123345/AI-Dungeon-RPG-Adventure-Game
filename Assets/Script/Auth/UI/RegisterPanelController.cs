using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// RegisterPanelController — Gắn vào LoginPanel trong scene Register.
///
/// Kết nối với (theo đúng tên trong Hierarchy của screenshot):
///   - InputField-email            → inputEmail
///   - InputField-username         → inputUsername
///   - InputField-password         → inputPassword
///   - InputField-confirm-password → inputConfirmPassword
///   - Register (Button)           → btnRegister
///   - Cancel   (Button)           → btnCancel
/// </summary>
public class RegisterPanelController : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField inputEmail;
    [SerializeField] private TMP_InputField inputUsername;
    [SerializeField] private TMP_InputField inputPassword;
    [SerializeField] private TMP_InputField inputConfirmPassword;

    [Header("Buttons")]
    [SerializeField] private Button btnRegister;
    [SerializeField] private Button btnCancel;
    [SerializeField] private Button btnGoToLogin; // Link "Đã có tài khoản? Đăng nhập"

    [Header("UI Feedback")]
    [Tooltip("Error text chung (hiển thị dưới form)")]
    [SerializeField] private TMP_Text txtError;

    [Tooltip("Error riêng cho từng field")]
    [SerializeField] private TMP_Text txtEmailError;
    [SerializeField] private TMP_Text txtUsernameError;
    [SerializeField] private TMP_Text txtPasswordError;
    [SerializeField] private TMP_Text txtConfirmPasswordError;

    [Tooltip("Loading overlay khi đang gọi API")]
    [SerializeField] private GameObject loadingIndicator;

    [Header("Scene Navigation")]
    [SerializeField] private string loginScene    = "Login";
    [SerializeField] private string mainMenuScene = "DemoMenu";
    [SerializeField] private string confirmScene  = ""; // Plan B: scene nhập OTP (để trống nếu chưa có)
    [SerializeField] private string welcomeScene  = "Welcome";

    // ── State ─────────────────────────────────────────────────────
    private bool   _isLoading;
    private string _pendingConfirmUsername; // Plan B: lưu username chờ OTP

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
        SetupButtons();
        SetupInputListeners();
        ClearAllErrors();
        SetLoading(false);

        if (inputEmail != null) inputEmail.Select();
    }

    private void OnDestroy()
    {
        if (AuthManager.Instance != null)
        {
            AuthManager.Instance.OnLoginSuccess          -= HandleRegisterSuccess;
            AuthManager.Instance.OnAuthFailed            -= HandleAuthFailed;
            AuthManager.Instance.OnConfirmationRequired  -= HandleConfirmationRequired;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // SETUP
    // ══════════════════════════════════════════════════════════════

    private void SetupButtons()
    {
        if (btnRegister != null)
        {
            btnRegister.onClick.RemoveAllListeners();
            btnRegister.onClick.AddListener(OnRegisterClicked);
        }

        if (btnCancel != null)
        {
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(() => SceneManager.LoadScene(welcomeScene));
        }

        if (btnGoToLogin != null)
        {
            btnGoToLogin.onClick.RemoveAllListeners();
            btnGoToLogin.onClick.AddListener(() => SceneManager.LoadScene(loginScene));
        }

        AuthManager.Instance.OnLoginSuccess         += HandleRegisterSuccess;
        AuthManager.Instance.OnAuthFailed           += HandleAuthFailed;
        AuthManager.Instance.OnConfirmationRequired += HandleConfirmationRequired;
    }

    private void SetupInputListeners()
    {
        // Tab qua fields: email → username → password → confirmPassword → submit
        if (inputEmail != null)
            inputEmail.onSubmit.AddListener(_ => inputUsername?.Select());

        if (inputUsername != null)
            inputUsername.onSubmit.AddListener(_ => inputPassword?.Select());

        if (inputPassword != null)
            inputPassword.onSubmit.AddListener(_ => inputConfirmPassword?.Select());

        if (inputConfirmPassword != null)
            inputConfirmPassword.onSubmit.AddListener(_ => OnRegisterClicked());

        // Real-time validation — confirm password
        if (inputConfirmPassword != null)
            inputConfirmPassword.onValueChanged.AddListener(val =>
            {
                if (!string.IsNullOrEmpty(val) && val != inputPassword?.text)
                    ShowFieldError(txtConfirmPasswordError, "Mật khẩu không khớp.");
                else
                    HideFieldError(txtConfirmPasswordError);
            });

        // Clear error on edit
        if (inputEmail != null)           inputEmail.onValueChanged.AddListener(_           => HideFieldError(txtEmailError));
        if (inputUsername != null)        inputUsername.onValueChanged.AddListener(_         => HideFieldError(txtUsernameError));
        if (inputPassword != null)        inputPassword.onValueChanged.AddListener(_         => HideFieldError(txtPasswordError));
    }

    // ══════════════════════════════════════════════════════════════
    // BUTTON HANDLERS
    // ══════════════════════════════════════════════════════════════

    private void OnRegisterClicked()
    {
        if (_isLoading) return;

        string email           = inputEmail?.text?.Trim() ?? "";
        string username        = inputUsername?.text?.Trim() ?? "";
        string password        = inputPassword?.text ?? "";
        string confirmPassword = inputConfirmPassword?.text ?? "";

        // Validate từng field
        bool valid = true;

        if (string.IsNullOrEmpty(email))
        {
            ShowFieldError(txtEmailError, "Vui lòng nhập email.");
            valid = false;
        }
        else if (!IsValidEmail(email))
        {
            ShowFieldError(txtEmailError, "Email không hợp lệ.");
            valid = false;
        }

        if (string.IsNullOrEmpty(username))
        {
            ShowFieldError(txtUsernameError, "Vui lòng nhập username.");
            valid = false;
        }
        else if (username.Length < 3)
        {
            ShowFieldError(txtUsernameError, "Username phải có ít nhất 3 ký tự.");
            valid = false;
        }

        if (string.IsNullOrEmpty(password))
        {
            ShowFieldError(txtPasswordError, "Vui lòng nhập mật khẩu.");
            valid = false;
        }
        else if (password.Length < 6)
        {
            ShowFieldError(txtPasswordError, "Mật khẩu phải có ít nhất 6 ký tự.");
            valid = false;
        }

        if (password != confirmPassword)
        {
            ShowFieldError(txtConfirmPasswordError, "Mật khẩu xác nhận không khớp.");
            valid = false;
        }

        if (!valid) return;

        _ = DoRegisterAsync(email, username, password, confirmPassword);
    }

    // ══════════════════════════════════════════════════════════════
    // AUTH FLOW
    // ══════════════════════════════════════════════════════════════

    private async Task DoRegisterAsync(string email, string username, string password, string confirmPassword)
    {
        SetLoading(true);
        ClearAllErrors();

        await AuthManager.Instance.RegisterAsync(username, email, password, confirmPassword);

        SetLoading(false);
    }

    private void HandleRegisterSuccess(GameShared.Models.User user)
    {
        Debug.Log($"[RegisterPanel] Register success: {user?.displayName}");
        SceneManager.LoadScene(mainMenuScene);
    }

    private void HandleAuthFailed(AuthResult result)
    {
        SetLoading(false);

        // Map error code sang đúng field
        switch (result.errorCode)
        {
            case "USERNAME_EXISTS":
                ShowFieldError(txtUsernameError, result.errorMessage);
                break;
            case "EMAIL_EXISTS":
                ShowFieldError(txtEmailError, result.errorMessage);
                break;
            case "PASSWORD_MISMATCH":
                ShowFieldError(txtConfirmPasswordError, result.errorMessage);
                break;
            case "WEAK_PASSWORD":
                ShowFieldError(txtPasswordError, result.errorMessage);
                break;
            default:
                ShowError(result.errorMessage ?? "Đăng ký thất bại. Vui lòng thử lại.");
                break;
        }
    }

    private void HandleConfirmationRequired(string username)
    {
        _pendingConfirmUsername = username;
        SetLoading(false);

        if (!string.IsNullOrEmpty(confirmScene))
        {
            // Plan B: redirect sang màn hình nhập OTP
            PlayerPrefs.SetString("pending_confirm_username", username);
            PlayerPrefs.Save();
            SceneManager.LoadScene(confirmScene);
        }
        else
        {
            // Hiển thị thông báo ngay trên form
            ShowError($"Tài khoản '{username}' đã được tạo!\nVui lòng kiểm tra email để xác nhận tài khoản trước khi đăng nhập.");
        }
    }

    // ══════════════════════════════════════════════════════════════
    // UI HELPERS
    // ══════════════════════════════════════════════════════════════

    private void SetLoading(bool loading)
    {
        _isLoading = loading;
        if (btnRegister != null)        btnRegister.interactable        = !loading;
        if (btnCancel != null)          btnCancel.interactable          = !loading;
        if (loadingIndicator != null)   loadingIndicator.SetActive(loading);
        if (inputEmail != null)         inputEmail.interactable         = !loading;
        if (inputUsername != null)      inputUsername.interactable      = !loading;
        if (inputPassword != null)      inputPassword.interactable      = !loading;
        if (inputConfirmPassword != null) inputConfirmPassword.interactable = !loading;
    }

    private void ShowError(string message)
    {
        if (txtError == null) { Debug.LogWarning($"[RegisterPanel] Error: {message}"); return; }
        txtError.text = message;
        txtError.gameObject.SetActive(true);
    }

    private void ShowFieldError(TMP_Text field, string message)
    {
        if (field == null) { ShowError(message); return; }
        field.text = message;
        field.gameObject.SetActive(true);
    }

    private void HideFieldError(TMP_Text field)
    {
        if (field != null) field.gameObject.SetActive(false);
    }

    private void ClearAllErrors()
    {
        HideFieldError(txtError);
        HideFieldError(txtEmailError);
        HideFieldError(txtUsernameError);
        HideFieldError(txtPasswordError);
        HideFieldError(txtConfirmPasswordError);
    }

    private static bool IsValidEmail(string email)
    {
        try { return new System.Net.Mail.MailAddress(email).Address == email; }
        catch { return false; }
    }
}
