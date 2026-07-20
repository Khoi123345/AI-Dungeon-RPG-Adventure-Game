using System;

namespace GameShared.DTOs.Inventory
{
    /// <summary>
    /// Response DTO cho API GET /items/{itemId}.
    /// Trả về thông tin chi tiết và chỉ số của một vật phẩm từ Item Catalog.
    /// </summary>
    [Serializable]
    public class ItemDetailResponse
    {
        public string itemId;
        /// <summary>Tên hiển thị của vật phẩm.</summary>
        public string name;
        /// <summary>Độ hiếm: "Common" | "Rare" | "Epic" | "Legendary"</summary>
        public string rarity;
        /// <summary>Loại vật phẩm: "Weapon" | "Armor" | "Accessory" | "Consumable"</summary>
        public string itemType;
        /// <summary>Slot trang bị: "MainHand" | "Chest" | "Head" | "Legs" | "Ring" | "Neck" (null cho Consumable)</summary>
        public string slotType;
        public int attackBonus;
        public int defenseBonus;
        public int hpBonus;
        public float criticalBonus;
        public string description;
        public string imageUrl;
        /// <summary>true nếu vật phẩm có thể cộng dồn số lượng trong 1 ô.</summary>
        public bool stackable;
        public int sellPrice;
        public int buyPrice;
        public int requiredLevel;
        /// <summary>
        /// JSON mô tả hiệu ứng khi sử dụng (chỉ dành cho Consumable).
        /// Ví dụ: {"hp": 50} hoặc {"attack": 10}
        /// </summary>
        public string effectJson;
    }
}
