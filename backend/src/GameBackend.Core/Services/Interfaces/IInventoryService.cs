using GameShared.DTOs.Inventory;

namespace GameBackend.Core.Services.Interfaces
{
    public interface IInventoryService
    {
        Task<InventoryResponse> GetInventoryAsync(string characterId);
        Task AddItemToInventoryAsync(string characterId, string itemId, int quantity);
    }
}
