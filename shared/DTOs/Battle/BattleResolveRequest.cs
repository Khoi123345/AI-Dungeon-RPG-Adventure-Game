using System;

namespace GameShared.DTOs.Battle
{
    [Serializable]
    public class BattleResolveRequest
    {
        public string characterId;
        public string encounterId;
        public System.Collections.Generic.List<string> equippedItemIds;
    }
}
