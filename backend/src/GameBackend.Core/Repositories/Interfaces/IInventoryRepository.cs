using GameShared.Models;

namespace GameBackend.Core.Repositories.Interfaces
{
    public interface IInventoryRepository
    {
        Task<List<Inventory>> GetByCharacterIdAsync(string characterId);
        Task<Inventory?> FindByCharacterAndItemAsync(string characterId, string itemId);
        Task SaveAsync(Inventory inventory);
    }
}
