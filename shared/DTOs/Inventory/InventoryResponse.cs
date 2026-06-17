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
        public string itemName;
        public string rarity;
        public string itemType;
        public int quantity;
        public bool equipped;
        public int slotIndex;
        public bool locked;
    }
}
