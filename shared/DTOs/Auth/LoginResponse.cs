using System;

namespace GameShared.DTOs.Auth
{
    /// <summary>
    /// DTO phản hồi sau khi đăng nhập hoặc đăng ký thành công.
    /// Dùng chung giữa Unity Client và AWS Lambda (Plan A và Plan B Cognito).
    /// </summary>
    [Serializable]
    public class LoginResponse
    {
        /// <summary>Access Token (JWT). Plan A: self-signed. Plan B: Cognito AccessToken (1h).</summary>
        public string token;

        /// <summary>Game User ID (DynamoDB PK).</summary>
        public string userId;

        /// <summary>Tên hiển thị trong game.</summary>
        public string displayName;

        /// <summary>Unix timestamp (giây) khi token hết hạn.</summary>
        public long expiresAt;

        /// <summary>Cognito Refresh Token (30 ngày). Chỉ có ở Plan B.</summary>
        public string refreshToken;

        /// <summary>Cognito ID Token (chứa user attributes). Chỉ có ở Plan B.</summary>
        public string idToken;

        /// <summary>
        /// Error code khi login/register thất bại.
        /// Ví dụ: "INVALID_CREDENTIALS", "USERNAME_EXISTS", "EMAIL_EXISTS", "USER_NOT_CONFIRMED".
        /// </summary>
        public string errorCode;
    }
}

