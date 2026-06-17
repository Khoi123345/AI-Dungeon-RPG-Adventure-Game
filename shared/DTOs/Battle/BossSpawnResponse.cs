using System;

namespace GameShared.DTOs.Battle
{
    [Serializable]
    public class BossSpawnResponse
    {
        public string encounterId;
        public string bossId;
        public string bossName;
        public string bossRarity;
        public int bossLevel;
        public int bossHp;
        public int bossAttack;
        public int bossDefense;
        public int bossSpeed;
        public float bossCriticalRate;
        public string bossImageUrl;
    }
}
