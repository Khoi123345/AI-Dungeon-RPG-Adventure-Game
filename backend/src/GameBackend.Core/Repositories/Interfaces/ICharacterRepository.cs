using GameShared.Models;

namespace GameBackend.Core.Repositories.Interfaces
{
    public interface ICharacterRepository
    {
        Task<Character?> GetByIdAsync(string characterId);
        Task<List<Character>> GetByUserIdAsync(string userId);
        Task SaveAsync(Character character);
    }
}
