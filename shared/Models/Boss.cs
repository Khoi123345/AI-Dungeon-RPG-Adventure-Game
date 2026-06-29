using System;

namespace GameShared.Models
{
    [Serializable]
    public class Boss
    {
        public string bossId { get; set; }
        public string name { get; set; }
        public string rarity { get; set; }
        public int level { get; set; }
        public int baseHp { get; set; }
        public int baseAttack { get; set; }
        public int baseDefense { get; set; }
        public int speed { get; set; }
        public float criticalRate { get; set; }
        public string imageUrl { get; set; }
        public int expReward { get; set; }
        public int goldReward { get; set; }
        public string skillSetJson { get; set; }
    }
}
