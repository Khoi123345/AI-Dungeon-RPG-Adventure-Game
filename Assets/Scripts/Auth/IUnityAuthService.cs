using System.Threading.Tasks;
using GameShared.Models;

/// <summary>
/// Interface cho Unity Auth Service.
/// Hai implementation: MockAuthService (Plan A) và RealAuthService (Plan B).
/// AuthManager chọn implementation dựa vào GameConfigSO.useMockMode.
/// </summary>
public interface IUnityAuthService
{
    /// <summary>True nếu người dùng đang đăng nhập (token còn hiệu lực).</summary>
    bool IsLoggedIn { get; }

    /// <summary>Access token hiện tại (Bearer token cho API calls).</summary>
    string CurrentToken { get; }

    /// <summary>Thông tin user đang đăng nhập.</summary>
    User CurrentUser { get; }

    /// <summary>Đăng nhập bằng username và password.</summary>
    Task<AuthResult> LoginAsync(string username, string password);

    /// <summary>Đăng ký tài khoản mới.</summary>
    Task<AuthResult> RegisterAsync(string username, string email, string password, string confirmPassword);

    /// <summary>
    /// Xác nhận OTP sau khi đăng ký (Plan B — Cognito).
    /// Plan A: luôn trả về success vì không cần confirm.
    /// </summary>
    Task<AuthResult> ConfirmSignUpAsync(string username, string confirmationCode);

    /// <summary>Đăng xuất, xóa token khỏi bộ nhớ và PlayerPrefs.</summary>
    Task LogoutAsync();

    /// <summary>
    /// Thử khôi phục session từ PlayerPrefs khi mở app.
    /// Trả về true nếu token còn hiệu lực và session đã được restore.
    /// </summary>
    Task<bool> TryRestoreSessionAsync();
}

/// <summary>
/// Kết quả trả về từ các thao tác Auth (Login, Register, Logout...).
/// </summary>
[System.Serializable]
public class AuthResult
{
    public bool success;

    /// <summary>
    /// Error code phân loại lỗi. Null nếu thành công.
    /// Giá trị: "INVALID_CREDENTIALS", "USERNAME_EXISTS", "EMAIL_EXISTS",
    ///          "PASSWORD_MISMATCH", "WEAK_PASSWORD", "USER_NOT_CONFIRMED",
    ///          "NETWORK_ERROR", "SERVER_ERROR".
    /// </summary>
    public string errorCode;

    /// <summary>Human-readable error message để hiển thị trên UI.</summary>
    public string errorMessage;

    /// <summary>Access Token (JWT). Lưu vào PlayerPrefs để persist session.</summary>
    public string token;

    /// <summary>Cognito Refresh Token (Plan B only). Dùng để lấy token mới khi hết hạn.</summary>
    public string refreshToken;

    /// <summary>Game User ID (DynamoDB PK).</summary>
    public string userId;

    /// <summary>Tên hiển thị trong game.</summary>
    public string displayName;

    /// <summary>Unix timestamp khi access token hết hạn.</summary>
    public long expiresAt;

    /// <summary>
    /// True nếu tài khoản cần xác nhận OTP qua email (Plan B — Cognito).
    /// UI sẽ redirect sang màn hình nhập OTP.
    /// </summary>
    public bool requiresConfirmation;

    // ── Helpers ────────────────────────────────────────────────────
    public static AuthResult Ok(string token, string userId, string displayName, long expiresAt, string refreshToken = null) => new AuthResult
    {
        success = true,
        token = token,
        userId = userId,
        displayName = displayName,
        expiresAt = expiresAt,
        refreshToken = refreshToken
    };

    public static AuthResult Fail(string errorCode, string message) => new AuthResult
    {
        success = false,
        errorCode = errorCode,
        errorMessage = message
    };

    public static AuthResult PendingConfirmation(string username) => new AuthResult
    {
        success = false,
        errorCode = "USER_NOT_CONFIRMED",
        errorMessage = "Tài khoản chưa được xác nhận. Vui lòng kiểm tra email.",
        requiresConfirmation = true
    };
}
