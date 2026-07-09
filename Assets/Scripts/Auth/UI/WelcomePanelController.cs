using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// WelcomePanelController - Gắn vào Welcome panel trong scene Welcome.
/// </summary>
public class WelcomePanelController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button btnLogin;
    [SerializeField] private Button btnRegister;

    [Header("Scene Navigation")]
    [SerializeField] private string loginSceneName = "Login";
    [SerializeField] private string registerSceneName = "Register";

    private void Start()
    {
        if (btnLogin != null)
        {
            btnLogin.onClick.RemoveAllListeners();
            btnLogin.onClick.AddListener(OnLoginClicked);
        }

        if (btnRegister != null)
        {
            btnRegister.onClick.RemoveAllListeners();
            btnRegister.onClick.AddListener(OnRegisterClicked);
        }
    }

    private void OnLoginClicked()
    {
        SceneManager.LoadScene(loginSceneName);
    }

    private void OnRegisterClicked()
    {
        SceneManager.LoadScene(registerSceneName);
    }
}
