using System;

namespace GameShared.DTOs.Auth
{
    /// <summary>
    /// DTO để refresh Access Token bằng Refresh Token (Plan B — Cognito).
    /// Route: POST /auth/refresh
    /// </summary>
    [Serializable]
    public class RefreshTokenRequest
    {
        public string refreshToken;
    }
}
