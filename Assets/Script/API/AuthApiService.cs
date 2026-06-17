using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// API service cho Authentication (Login/Register).
/// Gọi ApiClient để gửi request lên Lambda backend.
/// </summary>
public class AuthApiService
{
    public async Task<LoginResponseData> LoginAsync(string username, string password)
    {
        var body = new LoginRequestData { username = username, password = password };
        string json = await ApiClient.Instance.PostRawAsync("/auth/login", JsonUtility.ToJson(body));
        if (json == null) return null;
        return JsonUtility.FromJson<LoginResponseData>(json);
    }

    public async Task<LoginResponseData> RegisterAsync(string username, string email, string password)
    {
        var body = new RegisterRequestData { username = username, email = email, password = password };
        string json = await ApiClient.Instance.PostRawAsync("/auth/register", JsonUtility.ToJson(body));
        if (json == null) return null;
        return JsonUtility.FromJson<LoginResponseData>(json);
    }

    [Serializable]
    private class LoginRequestData
    {
        public string username;
        public string password;
    }

    [Serializable]
    private class RegisterRequestData
    {
        public string username;
        public string email;
        public string password;
    }

    [Serializable]
    public class LoginResponseData
    {
        public bool success;
        public string message;
        public LoginData data;
    }

    [Serializable]
    public class LoginData
    {
        public string token;
        public string userId;
        public string displayName;
    }
}
