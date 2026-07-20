using System;

namespace GameShared.DTOs.Inventory
{
    /// <summary>Request body cho POST /inventory/{characterId}/equip</summary>
    [Serializable]
    public class EquipItemRequest
    {
        /// <summary>inventoryId của bản ghi trong kho đồ cần trang bị.</summary>
        public string inventoryId;
    }

    /// <summary>Request body cho POST /inventory/{characterId}/unequip</summary>
    [Serializable]
    public class UnequipItemRequest
    {
        /// <summary>inventoryId của bản ghi trong kho đồ cần gỡ trang bị.</summary>
        public string inventoryId;
    }

    /// <summary>Request body cho POST /inventory/{characterId}/use</summary>
    [Serializable]
    public class UseItemRequest
    {
        /// <summary>inventoryId của bản ghi vật phẩm cần sử dụng.</summary>
        public string inventoryId;
        /// <summary>Số lượng muốn dùng. Mặc định = 1 nếu không truyền.</summary>
        public int quantityToUse = 1;
    }
}
