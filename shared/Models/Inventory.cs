using System;

namespace GameShared.Models
{
    [Serializable]
    public class Inventory
    {
        public string inventoryId { get; set; }
        public string characterId { get; set; }
        public string itemId { get; set; }
        public int quantity { get; set; }
        public bool equipped { get; set; }
        public int slotIndex { get; set; }
        public bool locked { get; set; }
        public DateTime acquiredAt { get; set; }
    }
}
