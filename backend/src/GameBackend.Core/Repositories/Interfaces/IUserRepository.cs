using GameShared.Models;

namespace GameBackend.Core.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string userId);
        Task<User?> GetByUsernameAsync(string username);
        /// <summary>Tìm user theo email (cho validate duplicate khi đăng ký).</summary>
        Task<User?> GetByEmailAsync(string email);
        /// <summary>
        /// Tìm user theo Cognito Sub ID (Plan B).
        /// Dùng để map Cognito identity sang game user profile trong DynamoDB.
        /// </summary>
        Task<User?> GetByCognitoSubAsync(string cognitoSub);
        Task SaveAsync(User user);
    }
}

