using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text txtUsername;
    [SerializeField] private Button btnLogout;
    [SerializeField] private Button btnPlay; // Nút Play

    private void Start()
    {
        // 1. Hiển thị tên người chơi từ GameProgressService
        if (txtUsername != null)
        {
            if (GameProgressService.Instance != null && GameProgressService.Instance.CurrentUser != null)
            {
                txtUsername.text = GameProgressService.Instance.CurrentUser.displayName;
            }
            else
            {
                txtUsername.text = "PLAYER: Guest";
            }
        }

        // 2. Gán sự kiện cho nút Logout
        if (btnLogout != null)
        {
            btnLogout.onClick.RemoveAllListeners();
            btnLogout.onClick.AddListener(OnLogoutClicked);
        }

        // 3. Gán sự kiện cho nút Play
        if (btnPlay != null)
        {
            btnPlay.onClick.RemoveAllListeners();
            btnPlay.onClick.AddListener(OnPlayClicked);
        }
    }

    private void OnPlayClicked()
    {
        Debug.Log("[MainMenuController] Chuyển sang BattleScene...");
        SceneManager.LoadScene("BattleScene");
    }

    private async void OnLogoutClicked()
    {
        if (AuthManager.Instance != null)
        {
            btnLogout.interactable = false; // Tránh spam nút
            await AuthManager.Instance.LogoutAsync();
        }
    }
}
