using GameShared.DTOs.Inventory;

namespace GameBackend.Core.Services.Interfaces
{
    public interface IInventoryService
    {
        /// <summary>Lấy toàn bộ kho đồ của nhân vật (kèm thông tin item từ catalog).</summary>
        Task<InventoryResponse> GetInventoryAsync(string characterId);

        /// <summary>Thêm item vào kho đồ, kiểm tra capacity và cộng dồn nếu đã có.</summary>
        Task AddItemToInventoryAsync(string characterId, string itemId, int quantity);

        /// <summary>Lấy thông tin chi tiết và stats của một item từ Item Catalog.</summary>
        Task<ItemDetailResponse?> GetItemDetailAsync(string itemId);

        /// <summary>
        /// Trang bị item. Tự động gỡ item cùng item_type đang equipped.
        /// Kiểm tra requiredLevel của item so với character level.
        /// </summary>
        Task<InventoryResponse> EquipItemAsync(string characterId, string inventoryId);

        /// <summary>Gỡ trang bị item (đặt equipped = false).</summary>
        Task<InventoryResponse> UnequipItemAsync(string characterId, string inventoryId);

        /// <summary>
        /// Sử dụng vật phẩm tiêu hao (Consumable). Apply effectJson lên Character.
        /// Xóa bản ghi nếu quantity về 0.
        /// </summary>
        Task<UseItemResponse> UseItemAsync(string characterId, string inventoryId, int quantityToUse);

        /// <summary>
        /// Cấp phát loot sau khi thắng Battle. Roll item rarity từ bossRarity,
        /// thêm vào kho đồ và ghi lịch sử vào LootDrop table.
        /// </summary>
        Task<List<LootItemDTO>> GrantLootDropAsync(string characterId, string bossRarity, string battleId);
    }
}
