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
        /// <summary>Độ hiếm của boss: "Common" | "Rare" | "Epic" | "Legendary" | "Mythic"</summary>
        public string bossRarity;
        public int playerHpBefore;
        public int playerHpAfter;
        public int bossHpBefore;
        public int bossHpAfter;
        public string status;
        public DateTime encounterTime;
    }
}
