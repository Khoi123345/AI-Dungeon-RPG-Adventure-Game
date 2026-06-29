using System;

namespace GameShared.DTOs.Auth
{
    /// <summary>
    /// DTO để xác nhận đăng ký bằng OTP code từ email (Plan B — Cognito).
    /// Route: POST /auth/confirm
    /// </summary>
    [Serializable]
    public class ConfirmSignUpRequest
    {
        public string username;
        public string confirmationCode;
    }
}
