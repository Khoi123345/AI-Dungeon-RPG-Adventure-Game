using System;

namespace GameShared.Models
{
    /// <summary>
    /// Domain model cho User account.
    /// passwordHash: dùng ở Plan A (self-managed auth).
    /// cognitoSub: dùng ở Plan B (Cognito là identity provider).
    /// </summary>
    [Serializable]
    public class User
    {
        public string userId { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        /// <summary>
        /// BCrypt hash của password. Chỉ dùng ở Plan A.
        /// Ở Plan B (Cognito), field này để trống — Cognito quản lý password.
        /// </summary>
        public string passwordHash { get; set; }
        public string displayName { get; set; }
        public string status { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime lastLoginAt { get; set; }
        /// <summary>
        /// Cognito User Sub (unique ID từ Cognito User Pool).
        /// Dùng ở Plan B để map Cognito identity sang game user profile trong DynamoDB.
        /// </summary>
        public string cognitoSub;
    }
}

