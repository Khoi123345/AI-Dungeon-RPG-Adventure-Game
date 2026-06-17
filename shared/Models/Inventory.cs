using System;

namespace GameShared.Models
{
    [Serializable]
    public class Inventory
    {
        public string inventoryId;
        public string characterId;
        public string itemId;
        public int quantity;
        public bool equipped;
        public int slotIndex;
        public bool locked;
        public DateTime acquiredAt;
    }
}
