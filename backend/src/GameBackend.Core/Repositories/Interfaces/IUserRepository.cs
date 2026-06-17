using GameShared.Models;

namespace GameBackend.Core.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string userId);
        Task<User?> GetByUsernameAsync(string username);
        Task SaveAsync(User user);
    }
}
