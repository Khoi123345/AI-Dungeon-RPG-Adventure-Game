using System;
using System.Threading.Tasks;
using GameShared.DTOs.Auth;
using GameShared.Models;
using UnityEngine;

/// <summary>
/// Real Auth Service — Plan A Online / Plan B Cognito.
/// Gọi ApiClient → AWS API Gateway → Lambda LoginHandler/RegisterHandler.
/// Xử lý token persistence và auto-refresh.
/// </summary>
public class RealAuthService : IUnityAuthService
{
    // ── PlayerPrefs Keys ──────────────────────────────────────────
    private const string KEY_AUTH_TOKEN    = "real_auth_token";
    private const string KEY_REFRESH_TOKEN = "real_auth_refresh_token";
    private const string KEY_AUTH_USER_ID  = "real_auth_userId";
    private const string KEY_AUTH_DISPLAY  = "real_auth_displayName";
    private const string KEY_AUTH_EXPIRES  = "real_auth_expiresAt";

    // ── State ─────────────────────────────────────────────────────
    private string _currentToken;
    private User   _currentUser;

    public bool   IsLoggedIn   => !string.IsNullOrEmpty(_currentToken);
    public string CurrentToken => _currentToken;
    public User   CurrentUser  => _currentUser;

    // ══════════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════════

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        try
        {
            var body = new LoginRequestPayload { username = username.Trim(), password = password };
            string json = await ApiClient.Instance.PostRawAsync("/auth/login", JsonUtility.ToJson(body));

            if (json == null)
                return AuthResult.Fail("NETWORK_ERROR", "Không thể kết nối đến server. Vui lòng kiểm tra mạng.");

            var wrapper = JsonUtility.FromJson<ApiResponseWrapper<LoginResponsePayload>>(json);

            if (wrapper == null || !wrapper.success)
            {
                string errCode = wrapper?.errorCode ?? "SERVER_ERROR";
                string errMsg  = MapErrorCode(errCode);
                return AuthResult.Fail(errCode, errMsg);
            }

            var data = wrapper.data;
            var user = new User
            {
                userId      = data.userId,
                username    = username.Trim(),
                displayName = data.displayName,
                status      = "Active"
            };

            SetSession(data.token, data.refreshToken, user, data.expiresAt);
            Debug.Log($"[RealAuth] Login success: {username}");
            return AuthResult.Ok(data.token, data.userId, data.displayName, data.expiresAt, data.refreshToken);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RealAuth] LoginAsync exception: {ex.Message}");
            return AuthResult.Fail("SERVER_ERROR", "Lỗi server. Vui lòng thử lại sau.");
        }
    }

    public async Task<AuthResult> RegisterAsync(string username, string email, string password, string confirmPassword)
    {
        if (password != confirmPassword)
            return AuthResult.Fail("PASSWORD_MISMATCH", "Mật khẩu xác nhận không khớp.");

        if (password.Length < 6)
            return AuthResult.Fail("WEAK_PASSWORD", "Mật khẩu phải có ít nhất 6 ký tự.");

        try
        {
            var body = new RegisterRequestPayload
            {
                username        = username.Trim(),
                email           = email.Trim().ToLower(),
                password        = password,
                confirmPassword = confirmPassword
            };

            string json = await ApiClient.Instance.PostRawAsync("/auth/register", JsonUtility.ToJson(body));

            if (json == null)
                return AuthResult.Fail("NETWORK_ERROR", "Không thể kết nối đến server.");

            var wrapper = JsonUtility.FromJson<ApiResponseWrapper<LoginResponsePayload>>(json);

            if (wrapper == null || !wrapper.success)
            {
                string errCode = wrapper?.errorCode ?? "SERVER_ERROR";

                // Plan B Cognito: user cần xác nhận email
                if (errCode == "USER_NOT_CONFIRMED")
                    return AuthResult.PendingConfirmation(username);

                return AuthResult.Fail(errCode, MapErrorCode(errCode));
            }

            var data = wrapper.data;
            var user = new User
            {
                userId      = data.userId,
                username    = username.Trim(),
                email       = email.Trim().ToLower(),
                displayName = data.displayName,
                status      = "Active"
            };

            // Nếu Cognito trả về token ngay (auto-confirm), lưu session
            if (!string.IsNullOrEmpty(data.token))
            {
                SetSession(data.token, data.refreshToken, user, data.expiresAt);
                return AuthResult.Ok(data.token, data.userId, data.displayName, data.expiresAt, data.refreshToken);
            }

            // Cognito yêu cầu xác nhận email trước
            Debug.Log($"[RealAuth] Register success, awaiting confirmation: {username}");
            return AuthResult.PendingConfirmation(username);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RealAuth] RegisterAsync exception: {ex.Message}");
            return AuthResult.Fail("SERVER_ERROR", "Lỗi server. Vui lòng thử lại sau.");
        }
    }

    public async Task<AuthResult> ConfirmSignUpAsync(string username, string confirmationCode)
    {
        try
        {
            var body = new ConfirmRequestPayload { username = username, confirmationCode = confirmationCode };
            string json = await ApiClient.Instance.PostRawAsync("/auth/confirm", JsonUtility.ToJson(body));

            if (json == null)
                return AuthResult.Fail("NETWORK_ERROR", "Không thể kết nối đến server.");

            var wrapper = JsonUtility.FromJson<ApiResponseWrapper<LoginResponsePayload>>(json);

            if (wrapper == null || !wrapper.success)
                return AuthResult.Fail(wrapper?.errorCode ?? "SERVER_ERROR", "Xác nhận thất bại.");

            Debug.Log($"[RealAuth] ConfirmSignUp success: {username}");
            return new AuthResult { success = true };
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RealAuth] ConfirmSignUpAsync exception: {ex.Message}");
            return AuthResult.Fail("SERVER_ERROR", "Lỗi server khi xác nhận tài khoản.");
        }
    }

    public Task LogoutAsync()
    {
        _currentToken = null;
        _currentUser  = null;
        PlayerPrefs.DeleteKey(KEY_AUTH_TOKEN);
        PlayerPrefs.DeleteKey(KEY_REFRESH_TOKEN);
        PlayerPrefs.DeleteKey(KEY_AUTH_USER_ID);
        PlayerPrefs.DeleteKey(KEY_AUTH_DISPLAY);
        PlayerPrefs.DeleteKey(KEY_AUTH_EXPIRES);
        PlayerPrefs.Save();
        ApiClient.Instance.ClearAuth();
        Debug.Log("[RealAuth] Logged out.");
        return Task.CompletedTask;
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        await Task.Yield();

        string token = PlayerPrefs.GetString(KEY_AUTH_TOKEN, null);
        if (string.IsNullOrEmpty(token)) return false;

        long expiresAt = long.Parse(PlayerPrefs.GetString(KEY_AUTH_EXPIRES, "0"));

        // Nếu token gần hết hạn (< 5 phút còn lại), thử refresh
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now >= expiresAt - 300)
        {
            string refreshToken = PlayerPrefs.GetString(KEY_REFRESH_TOKEN, null);
            if (!string.IsNullOrEmpty(refreshToken))
            {
                bool refreshed = await TryRefreshTokenAsync(refreshToken);
                if (!refreshed)
                {
                    await LogoutAsync();
                    return false;
                }
                return true;
            }
            await LogoutAsync();
            return false;
        }

        string userId      = PlayerPrefs.GetString(KEY_AUTH_USER_ID, null);
        string displayName = PlayerPrefs.GetString(KEY_AUTH_DISPLAY, null);

        _currentToken = token;
        _currentUser  = new User { userId = userId, displayName = displayName, status = "Active" };
        ApiClient.Instance.SetAuth(token);

        Debug.Log($"[RealAuth] Session restored for userId={userId}");
        return true;
    }

    // ══════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════

    private async Task<bool> TryRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var body = new RefreshRequestPayload { refreshToken = refreshToken };
            string json = await ApiClient.Instance.PostRawAsync("/auth/refresh", JsonUtility.ToJson(body));
            if (json == null) return false;

            var wrapper = JsonUtility.FromJson<ApiResponseWrapper<LoginResponsePayload>>(json);
            if (wrapper == null || !wrapper.success) return false;

            var data = wrapper.data;
            string newRefresh = string.IsNullOrEmpty(data.refreshToken) ? refreshToken : data.refreshToken;

            var user = new User
            {
                userId      = data.userId,
                displayName = data.displayName,
                status      = "Active"
            };

            SetSession(data.token, newRefresh, user, data.expiresAt);
            Debug.Log("[RealAuth] Token refreshed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[RealAuth] TryRefreshToken exception: {ex.Message}");
            return false;
        }
    }

    private void SetSession(string token, string refreshToken, User user, long expiresAt)
    {
        _currentToken = token;
        _currentUser  = user;

        PlayerPrefs.SetString(KEY_AUTH_TOKEN,    token);
        PlayerPrefs.SetString(KEY_AUTH_USER_ID,  user.userId);
        PlayerPrefs.SetString(KEY_AUTH_DISPLAY,  user.displayName);
        PlayerPrefs.SetString(KEY_AUTH_EXPIRES,  expiresAt.ToString());

        if (!string.IsNullOrEmpty(refreshToken))
            PlayerPrefs.SetString(KEY_REFRESH_TOKEN, refreshToken);

        PlayerPrefs.Save();
        ApiClient.Instance.SetAuth(token);
    }

    private static string MapErrorCode(string code) => code switch
    {
        "INVALID_CREDENTIALS"  => "Username hoặc mật khẩu không đúng.",
        "USERNAME_EXISTS"      => "Username đã được sử dụng.",
        "EMAIL_EXISTS"         => "Email đã được đăng ký.",
        "USER_NOT_FOUND"       => "Tài khoản không tồn tại.",
        "ACCOUNT_DISABLED"     => "Tài khoản đã bị vô hiệu hóa.",
        "USER_NOT_CONFIRMED"   => "Tài khoản chưa được xác nhận. Kiểm tra email.",
        "WEAK_PASSWORD"        => "Mật khẩu quá yếu. Cần ít nhất 8 ký tự, có số.",
        _                      => "Đã có lỗi xảy ra. Vui lòng thử lại."
    };

    // ── Serializable Payloads ─────────────────────────────────────

    [Serializable] private class LoginRequestPayload   { public string username; public string password; }
    [Serializable] private class RegisterRequestPayload { public string username; public string email; public string password; public string confirmPassword; }
    [Serializable] private class ConfirmRequestPayload { public string username; public string confirmationCode; }
    [Serializable] private class RefreshRequestPayload { public string refreshToken; }

    [Serializable]
    private class LoginResponsePayload
    {
        public string token;
        public string refreshToken;
        public string idToken;
        public string userId;
        public string displayName;
        public long   expiresAt;
        public string errorCode;
    }

    [Serializable]
    private class ApiResponseWrapper<T> where T : class
    {
        public bool   success;
        public string message;
        public string errorCode;
        public T      data;
    }
}
