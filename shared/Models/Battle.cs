using System;

namespace GameShared.Models
{
    [Serializable]
    public class Battle
    {
        public string battleId { get; set; }
        public string encounterId { get; set; }
        public int playerPower { get; set; }
        public int bossPower { get; set; }
        public string battleType { get; set; }
        public string status { get; set; }
        public string result { get; set; }
        public int turnCount { get; set; }
        public int durationMs { get; set; }
        public string playerSnapshotJson { get; set; }
        public string bossSnapshotJson { get; set; }
        public string rewardJson { get; set; }
        public DateTime battleTime { get; set; }
    }
}
