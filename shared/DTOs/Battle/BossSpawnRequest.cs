using System;

namespace GameShared.DTOs.Battle
{
    [Serializable]
    public class BossSpawnRequest
    {
        public string characterId;
        public string sessionId;
    }
}
