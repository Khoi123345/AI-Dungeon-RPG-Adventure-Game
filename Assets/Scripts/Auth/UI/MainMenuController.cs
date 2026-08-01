using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text txtUsername;
    [SerializeField] private Button btnLogout;
    [SerializeField] private Button btnPlay;
    [SerializeField] private Button btnShop;
    [SerializeField] private Button btnProfile;
    // btnInventory: tạm thời chưa làm, để trống trong Inspector

    [Header("Scene Names")]
    [SerializeField] private string storyScene    = "StoryScene";
    [SerializeField] private string shopScene     = "Shop";
    [SerializeField] private string profileScene  = "Profile";

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

        // 2. Nút Logout
        if (btnLogout != null)
        {
            btnLogout.onClick.RemoveAllListeners();
            btnLogout.onClick.AddListener(OnLogoutClicked);
        }

        // 3. Nút Play → StoryScene
        if (btnPlay != null)
        {
            btnPlay.onClick.RemoveAllListeners();
            btnPlay.onClick.AddListener(OnPlayClicked);
        }

        // 4. Nút Shop → ShopScene
        if (btnShop != null)
        {
            btnShop.onClick.RemoveAllListeners();
            btnShop.onClick.AddListener(() => SceneManager.LoadScene(shopScene));
        }

        // 5. Nút Profile → ProfileScene (lưu scene hiện tại để Back)
        if (btnProfile != null)
        {
            btnProfile.onClick.RemoveAllListeners();
            btnProfile.onClick.AddListener(OnProfileClicked);
        }
    }

    private void OnPlayClicked()
    {
        Debug.Log("[MainMenuController] Chuyển sang StoryScene...");
        SceneManager.LoadScene(storyScene);
    }

    private void OnProfileClicked()
    {
        ProfilePresenter.SetPreviousScene(SceneManager.GetActiveScene().name);
        Debug.Log("[MainMenuController] Chuyển sang ProfileScene...");
        SceneManager.LoadScene(profileScene);
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
