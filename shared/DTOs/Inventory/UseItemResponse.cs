using System;

namespace GameShared.DTOs.Inventory
{
    /// <summary>
    /// Response DTO cho POST /inventory/{characterId}/use.
    /// Trả về trạng thái vật phẩm sau khi dùng và chỉ số nhân vật được cập nhật.
    /// </summary>
    [Serializable]
    public class UseItemResponse
    {
        public string inventoryId;
        public string itemId;
        public string itemName;
        /// <summary>Số lượng còn lại trong kho sau khi dùng.</summary>
        public int quantityRemaining;
        /// <summary>true nếu bản ghi inventory đã bị xóa (quantity về 0).</summary>
        public bool itemDeleted;
        /// <summary>Chỉ số nhân vật sau khi hiệu ứng được áp dụng.</summary>
        public UpdatedCharacterStats updatedStats;
    }

    /// <summary>Snapshot chỉ số nhân vật sau khi dùng vật phẩm.</summary>
    [Serializable]
    public class UpdatedCharacterStats
    {
        public int hp;
        public int maxHp;
        public int attack;
        public int defense;
        public float criticalRate;
        public int gold;
    }
}
