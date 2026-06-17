using System;

namespace GameShared.DTOs.Battle
{
    [Serializable]
    public class BattleResolveRequest
    {
        public string characterId;
        public string encounterId;
    }
}
