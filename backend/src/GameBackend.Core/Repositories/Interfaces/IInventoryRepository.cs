using GameShared.Models;

namespace GameBackend.Core.Repositories.Interfaces
{
    public interface IInventoryRepository
    {
        /// <summary>Lấy toàn bộ inventory của một nhân vật.</summary>
        Task<List<Inventory>> GetByCharacterIdAsync(string characterId);

        /// <summary>Tìm bản ghi inventory theo inventoryId (Partition Key).</summary>
        Task<Inventory?> GetByInventoryIdAsync(string inventoryId);

        /// <summary>Tìm bản ghi inventory theo characterId + itemId (dùng khi add/stack item).</summary>
        Task<Inventory?> FindByCharacterAndItemAsync(string characterId, string itemId);

        /// <summary>Lấy danh sách tất cả item đang equipped (equipped = true) của một nhân vật.</summary>
        Task<List<Inventory>> GetEquippedItemsAsync(string characterId);

        /// <summary>
        /// Đếm số ô đang sử dụng (số bản ghi có quantity > 0) của một nhân vật.
        /// Dùng để kiểm tra giới hạn kho đồ (Max_Slots = 100).
        /// </summary>
        Task<int> CountSlotsAsync(string characterId);

        /// <summary>Lưu hoặc cập nhật một bản ghi inventory (PutItem).</summary>
        Task SaveAsync(Inventory inventory);

        /// <summary>Xóa hẳn bản ghi inventory (dùng khi quantity = 0 sau UseItem).</summary>
        Task DeleteAsync(string inventoryId);
    }
}
