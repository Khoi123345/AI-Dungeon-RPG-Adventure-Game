using System;

namespace GameShared.Models
{
    [Serializable]
    public class LootDrop
    {
        public string lootId { get; set; }
        public string battleId { get; set; }
        public string itemId { get; set; }
        public int quantity { get; set; }
        public float dropRate { get; set; }
        public string sourceType { get; set; }
        public bool isUnique { get; set; }
        public DateTime createdAt { get; set; }
    }
}
