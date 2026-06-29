using System;

namespace GameShared.DTOs.Auth
{
    /// <summary>
    /// DTO cho request đăng ký tài khoản mới.
    /// Dùng chung giữa Unity Client và AWS Lambda RegisterHandler.
    /// </summary>
    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string email;
        public string password;
        public string confirmPassword;
    }
}
