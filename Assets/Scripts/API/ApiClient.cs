using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// HTTP client wrapper cho UnityWebRequest.
/// Tự động đính kèm JWT token, xử lý retry, và parse JSON response.
/// Singleton pattern (DontDestroyOnLoad).
/// </summary>
public class ApiClient : MonoBehaviour
{
    private static ApiClient instance;
    public static ApiClient Instance
    {
        get
        {
            if (instance == null) EnsureInstance();
            return instance;
        }
    }

    private string authToken;
    private string baseUrl;

    public bool IsAuthenticated => !string.IsNullOrEmpty(authToken);

    public static ApiClient EnsureInstance()
    {
        if (instance != null) return instance;
        GameObject go = new GameObject(nameof(ApiClient));
        instance = go.AddComponent<ApiClient>();
        DontDestroyOnLoad(go);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Đọc cấu hình từ GameConfigSO trong thư mục Resources
        GameConfigSO config = Resources.Load<GameConfigSO>("GameConfig");
        
        if (config != null && !string.IsNullOrEmpty(config.apiBaseUrl))
        {
            baseUrl = config.apiBaseUrl;
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
        }
        else
        {
            Debug.LogWarning("[ApiClient] Không tìm thấy file 'GameConfig' trong thư mục Resources (hoặc apiBaseUrl trống). Đang dùng URL mặc định.");
            baseUrl = "https://ne6hi09ope.execute-api.ap-southeast-1.amazonaws.com/prod/";
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void SetAuth(string token) => authToken = token;
    public void ClearAuth() => authToken = null;
    public void SetBaseUrl(string url) => baseUrl = url;

    // ══════════════════════════════════════════════════════
    // PUBLIC HTTP METHODS
    // ══════════════════════════════════════════════════════

    public async Task<T> GetAsync<T>(string path) where T : class
    {
        string json = await GetRawAsync(path);
        if (json == null) return null;
        return JsonUtility.FromJson<T>(json);
    }

    public async Task<T> PostAsync<T>(string path, object body) where T : class
    {
        string json = await PostRawAsync(path, JsonUtility.ToJson(body));
        if (json == null) return null;
        return JsonUtility.FromJson<T>(json);
    }

    public async Task<string> GetRawAsync(string path)
    {
        string url = baseUrl + path;
        using UnityWebRequest request = UnityWebRequest.Get(url);
        AddHeaders(request);
        return await SendRequestAsync(request, path);
    }

    public async Task<string> PostRawAsync(string path, string jsonBody)
    {
        string url = baseUrl + path;
        byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

        using UnityWebRequest request = new UnityWebRequest(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(bodyBytes),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");
        AddHeaders(request);

        return await SendRequestAsync(request, path);
    }

    // ══════════════════════════════════════════════════════
    // INTERNALS
    // ══════════════════════════════════════════════════════

    private async Task<string> SendRequestAsync(UnityWebRequest request, string path)
    {
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        while (!operation.isDone)
        {
            await Task.Yield();
        }

        // Lỗi kết nối mạng (mất mạng, DNS...)
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogError($"[ApiClient] {request.method} {path} connection failed: {request.error}");
            return null;
        }

        // Lỗi giao thức HTTP (400, 409...) nhưng server vẫn trả về JSON body thông báo lỗi
        if (request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogWarning($"[ApiClient] {request.method} {path} protocol error: {request.responseCode} | {request.downloadHandler?.text}");
            return request.downloadHandler?.text; // Trả về JSON để RealAuthService parse errorCode
        }

        return request.downloadHandler.text;
    }

    private void AddHeaders(UnityWebRequest request)
    {
        if (!string.IsNullOrEmpty(authToken))
        {
            request.SetRequestHeader("Authorization", "Bearer " + authToken);
        }
    }
}
