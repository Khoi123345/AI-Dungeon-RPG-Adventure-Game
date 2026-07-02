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
        public string userId;
        public string username;
        public string email;
        /// <summary>
        /// BCrypt hash của password. Chỉ dùng ở Plan A.
        /// Ở Plan B (Cognito), field này để trống — Cognito quản lý password.
        /// </summary>
        public string passwordHash;
        public string displayName;
        public string status;
        public DateTime createdAt;
        public DateTime lastLoginAt;
        /// <summary>
        /// Cognito User Sub (unique ID từ Cognito User Pool).
        /// Dùng ở Plan B để map Cognito identity sang game user profile trong DynamoDB.
        /// </summary>
        public string cognitoSub;
    }
}

