using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ConfirmSignUpController — Gắn vào ConfirmSignUpPanel trong scene Register/Confirm.
/// Nhận username và OTP code từ email để xác nhận tài khoản Cognito.
/// </summary>
public class ConfirmSignUpController : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField inputUsername;
    [SerializeField] private TMP_InputField inputCode;

    [Header("Buttons")]
    [SerializeField] private Button btnConfirm;
    [SerializeField] private Button btnCancel;

    [Header("UI Feedback")]
    [SerializeField] private TMP_Text txtError;
    [SerializeField] private GameObject loadingIndicator;

    [Header("Scene Navigation")]
    [SerializeField] private string loginScene = "Login";

    private bool _isLoading;

    private void Awake()
    {
        AuthManager.EnsureInstance();
    }

    private void Start()
    {
        SetupButtons();
    }

    private void OnEnable()
    {
        HideError();
        SetLoading(false);

        // Pre-fill username from Register if available
        string pendingUsername = PlayerPrefs.GetString("pending_confirm_username", "");
        if (inputUsername != null && !string.IsNullOrEmpty(pendingUsername))
        {
            inputUsername.text = pendingUsername;
            if (inputCode != null) inputCode.Select();
        }
        else if (inputUsername != null)
        {
            inputUsername.Select();
        }
    }

    private void SetupButtons()
    {
        if (btnConfirm != null)
        {
            btnConfirm.onClick.RemoveAllListeners();
            btnConfirm.onClick.AddListener(OnConfirmClicked);
        }

        if (btnCancel != null)
        {
            btnCancel.onClick.RemoveAllListeners();
            btnCancel.onClick.AddListener(() => SceneManager.LoadScene(loginScene));
        }
    }

    private void OnConfirmClicked()
    {
        if (_isLoading) return;

        string username = inputUsername?.text?.Trim() ?? "";
        string code = inputCode?.text?.Trim() ?? "";

        if (string.IsNullOrEmpty(username))
        {
            ShowError("Vui lòng nhập username.");
            return;
        }

        if (string.IsNullOrEmpty(code))
        {
            ShowError("Vui lòng nhập mã OTP.");
            return;
        }

        _ = DoConfirmAsync(username, code);
    }

    private async Task DoConfirmAsync(string username, string code)
    {
        SetLoading(true);
        HideError();

        var result = await AuthManager.Instance.ConfirmSignUpAsync(username, code);

        SetLoading(false);

        if (result.success)
        {
            // Clear pending username
            PlayerPrefs.DeleteKey("pending_confirm_username");
            PlayerPrefs.Save();
            
            // Redirect to Login scene with success message
            Debug.Log("[ConfirmSignUp] Account confirmed successfully. Redirecting to Login.");
            SceneManager.LoadScene(loginScene);
        }
        else
        {
            ShowError(result.errorMessage ?? "Mã xác nhận không đúng hoặc đã hết hạn.");
        }
    }

    private void SetLoading(bool loading)
    {
        _isLoading = loading;
        if (btnConfirm != null) btnConfirm.interactable = !loading;
        if (btnCancel != null) btnCancel.interactable = !loading;
        if (loadingIndicator != null) loadingIndicator.SetActive(loading);
        if (inputUsername != null) inputUsername.interactable = !loading;
        if (inputCode != null) inputCode.interactable = !loading;
    }

    private void ShowError(string message)
    {
        if (txtError == null) { Debug.LogWarning($"[ConfirmSignUp] Error: {message}"); return; }
        txtError.text = message;
        txtError.gameObject.SetActive(true);
    }

    private void HideError()
    {
        if (txtError != null) txtError.gameObject.SetActive(false);
    }
}
