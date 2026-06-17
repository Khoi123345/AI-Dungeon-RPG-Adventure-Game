using System;

namespace GameShared.Models
{
    [Serializable]
    public class BossEncounter
    {
        public string encounterId;
        public string characterId;
        public string bossId;
        public int bossLevel;
        public int playerHpBefore;
        public int playerHpAfter;
        public int bossHpBefore;
        public int bossHpAfter;
        public string status;
        public DateTime encounterTime;
    }
}
