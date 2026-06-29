using System;

namespace GameShared.Models
{
    [Serializable]
    public class User
    {
        public string userId { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public string passwordHash { get; set; }
        public string displayName { get; set; }
        public string status { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime lastLoginAt { get; set; }
    }
}
