using System;

namespace GameShared.DTOs.Battle
{
    [Serializable]
    public class BossSpawnRequest
    {
        public string characterId;
        public string sessionId;
        public string bossId;
        public string encounterId;
        public int bossLevel;
    }
}
