using UnityEngine;

/// <summary>
/// ScriptableObject chứa cấu hình game.
/// Tạo asset trong Unity Editor: Assets > Create > Game > GameConfig
/// Cho phép chuyển đổi Mock Mode / Online Mode từ Inspector.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/GameConfig")]
public class GameConfigSO : ScriptableObject
{
    [Header("API Settings")]
    [Tooltip("Base URL của API Gateway (ví dụ: https://xxx.execute-api.ap-southeast-1.amazonaws.com/prod)")]
    public string apiBaseUrl = "https://your-api-gateway-url.execute-api.ap-southeast-1.amazonaws.com/prod";

    [Tooltip("Timeout cho mỗi API call (giây)")]
    public float apiTimeoutSeconds = 30f;

    [Header("Mode Settings")]
    [Tooltip("Bật Mock Mode: game chạy offline, không gọi API, dùng dữ liệu giả từ GameProgressService")]
    public bool useMockMode = true;

    [Header("Debug")]
    [Tooltip("Log tất cả API request/response ra Console")]
    public bool enableApiLogging = true;

    // Singleton instance — load từ Resources folder
    private static GameConfigSO _instance;
    public static GameConfigSO Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameConfigSO>("GameConfig");
                if (_instance == null)
                {
                    Debug.LogWarning("[GameConfigSO] GameConfig asset not found in Resources. Using defaults.");
                    _instance = CreateInstance<GameConfigSO>();
                }
            }
            return _instance;
        }
    }
}
