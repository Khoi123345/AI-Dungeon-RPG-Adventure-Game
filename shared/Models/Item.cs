using System;

namespace GameShared.Models
{
    [Serializable]
    public class Item
    {
        public string itemId;
        public string name;
        public string rarity;
        public string itemType;
        public int attackBonus;
        public int defenseBonus;
        public int hpBonus;
        public float criticalBonus;
        public string imageUrl;
        public string description;
        public bool stackable;
        public int sellPrice;
        public int buyPrice;
        public int requiredLevel;
        public string slotType;
        public string effectJson;
    }
}
