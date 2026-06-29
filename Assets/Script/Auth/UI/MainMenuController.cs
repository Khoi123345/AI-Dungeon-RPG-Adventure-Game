using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text txtUsername;
    [SerializeField] private Button btnLogout;

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
