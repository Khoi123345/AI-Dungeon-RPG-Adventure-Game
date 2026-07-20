using System;

namespace GameShared.Models
{
    [Serializable]
    public class BossEncounter
    {
        public string encounterId { get; set; }
        public string characterId { get; set; }
        public string bossId { get; set; }
        public int bossLevel { get; set; }
        /// <summary>Độ hiếm của boss: "Common" | "Rare" | "Epic" | "Legendary" | "Mythic"</summary>
        public string bossRarity { get; set; }
        public int playerHpBefore { get; set; }
        public int playerHpAfter { get; set; }
        public int bossHpBefore { get; set; }
        public int bossHpAfter { get; set; }
        public string status { get; set; }
        public DateTime encounterTime { get; set; }
    }
}
