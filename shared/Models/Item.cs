using System;

namespace GameShared.Models
{
    [Serializable]
    public class Item
    {
        public string itemId { get; set; }
        public string name { get; set; }
        public string rarity { get; set; }
        public string itemType { get; set; }
        public int attackBonus { get; set; }
        public int defenseBonus { get; set; }
        public int hpBonus { get; set; }
        public float criticalBonus { get; set; }
        public string imageUrl { get; set; }
        public string description { get; set; }
        public bool stackable { get; set; }
        public int sellPrice { get; set; }
        public int buyPrice { get; set; }
        public int requiredLevel { get; set; }
        public string slotType { get; set; }
        public string effectJson { get; set; }
    }
}
