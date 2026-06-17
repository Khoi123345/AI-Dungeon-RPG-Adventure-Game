using System;

namespace GameShared.Models
{
    [Serializable]
    public class Boss
    {
        public string bossId;
        public string name;
        public string rarity;
        public int level;
        public int baseHp;
        public int baseAttack;
        public int baseDefense;
        public int speed;
        public float criticalRate;
        public string imageUrl;
        public int expReward;
        public int goldReward;
        public string skillSetJson;
    }
}
