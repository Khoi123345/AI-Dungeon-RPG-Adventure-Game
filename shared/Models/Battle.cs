using System;

namespace GameShared.Models
{
    [Serializable]
    public class Battle
    {
        public string battleId;
        public string encounterId;
        public int playerPower;
        public int bossPower;
        public string battleType;
        public string status;
        public string result;
        public int turnCount;
        public int durationMs;
        public string playerSnapshotJson;
        public string bossSnapshotJson;
        public string rewardJson;
        public DateTime battleTime;
    }
}
