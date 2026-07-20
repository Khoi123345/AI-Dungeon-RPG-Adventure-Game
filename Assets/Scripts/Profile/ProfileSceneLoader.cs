using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Attach lên Button "Profile" ở menu scene hoặc bất kỳ scene nào muốn mở ProfileScene.
/// 
/// SETUP:
///   1. Tạo một Button trong scene menu (hoặc StoryScene).
///   2. Attach script này lên Button GameObject đó.
///   3. HOẶC gọi OpenProfileScene() từ onClick event trong Inspector.
///   4. Đảm bảo "ProfileScene" đã được thêm vào Build Settings
///      (File > Build Settings > Add Open Scenes).
/// </summary>
public class ProfileSceneLoader : MonoBehaviour
{
    [Tooltip("Tên scene ProfileScene trong Build Settings. Mặc định: 'ProfileScene'")]
    [SerializeField] private string profileSceneName = "Profile";

    private void Awake()
    {
        // Nếu có Button trên cùng GameObject, tự bind
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OpenProfileScene);
        }
    }

    /// <summary>
    /// Lưu scene hiện tại và load ProfileScene.
    /// Gọi từ onClick Button hoặc code bất kỳ.
    /// </summary>
    public void OpenProfileScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        ProfilePresenter.SetPreviousScene(currentScene);

        Debug.Log($"[ProfileSceneLoader] Mở ProfileScene. Scene trước: {currentScene}");
        SceneManager.LoadScene(profileSceneName);
    }
}
