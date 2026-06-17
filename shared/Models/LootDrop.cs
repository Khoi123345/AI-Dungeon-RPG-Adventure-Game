using System;

namespace GameShared.Models
{
    [Serializable]
    public class LootDrop
    {
        public string lootId;
        public string battleId;
        public string itemId;
        public int quantity;
        public float dropRate;
        public string sourceType;
        public bool isUnique;
        public DateTime createdAt;
    }
}
