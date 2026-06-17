using System;

namespace GameShared.Models
{
    [Serializable]
    public class User
    {
        public string userId;
        public string username;
        public string email;
        public string passwordHash;
        public string displayName;
        public string status;
        public DateTime createdAt;
        public DateTime lastLoginAt;
    }
}
