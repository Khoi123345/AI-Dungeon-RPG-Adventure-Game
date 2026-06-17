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

        // TODO: Đọc từ GameConfigSO ScriptableObject
        baseUrl = "https://your-api-gateway-url.execute-api.ap-southeast-1.amazonaws.com/prod";
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

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ApiClient] {request.method} {path} failed: {request.error} | {request.downloadHandler?.text}");
            return null;
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
