using System;

namespace GameShared.DTOs.Auth
{
    [Serializable]
    public class LoginResponse
    {
        public string token;
        public string userId;
        public string displayName;
        public long expiresAt;
    }
}
