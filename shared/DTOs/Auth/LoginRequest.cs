using System;

namespace GameShared.DTOs.Auth
{
    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }
}
