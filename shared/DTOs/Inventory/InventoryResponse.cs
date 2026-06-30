using System;
using System.Collections.Generic;

namespace GameShared.DTOs.Inventory
{
    [Serializable]
    public class InventoryResponse
    {
        public string characterId;
        public List<InventorySlot> slots;
        public int totalSlots;
    }

    [Serializable]
    public class InventorySlot
    {
        public string inventoryId;
        public string itemId;
        /// <summary>Tên vật phẩm (lấy từ ItemCatalog khi build response).</summary>
        public string itemName;
        /// <summary>Loại vật phẩm: "Weapon" | "Armor" | "Accessory" | "Consumable"</summary>
        public string itemType;
        public string rarity;
        public int quantity;
        public bool equipped;
        public int slotIndex;
        public bool locked;
        // Stats để hiển thị tooltip trong client
        public int attackBonus;
        public int defenseBonus;
        public int hpBonus;
        public float criticalBonus;
    }
}
