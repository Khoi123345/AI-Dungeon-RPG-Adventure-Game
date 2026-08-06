using System;

namespace GameShared.Models
{
    [Serializable]
    public class DefeatedBoss
    {
        public string characterId { get; set; }
        public string bossId { get; set; }
        public string bossName { get; set; }
        public int bossLevel { get; set; }
        public string encounterId { get; set; }
        public DateTime defeatedAt { get; set; }
    }
}
