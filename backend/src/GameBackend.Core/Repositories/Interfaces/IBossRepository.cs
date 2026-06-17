using GameShared.Models;

namespace GameBackend.Core.Repositories.Interfaces
{
    public interface IBossRepository
    {
        Task<Boss?> GetByIdAsync(string bossId);
        Task SaveAsync(Boss boss);
    }
}
