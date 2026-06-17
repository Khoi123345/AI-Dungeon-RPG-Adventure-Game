using System;
using System.Collections.Generic;
using GameShared.DTOs.Character;

namespace GameShared.DTOs.Battle
{
    /// <summary>
    /// Kết quả trận chiến được server tính toán.
    /// Client nhận dữ liệu này để phát lại (playback) diễn biến trên UI.
    /// </summary>
    [Serializable]
    public class BattleResolveResponse
    {
        public string battleId;
        public string encounterId;
        public bool isPlayerVictory;
        public List<BattleTurnData> turns;
        public BattleRewardData rewards;
        public CharacterResponse updatedCharacter;
    }

    [Serializable]
    public class BattleTurnData
    {
        public string attackerName;
        public string logMessage;
        public int damage;
        public int playerHpRemaining;
        public int bossHpRemaining;
        public bool isCritical;
    }

    [Serializable]
    public class BattleRewardData
    {
        public int goldEarned;
        public int expEarned;
        public List<LootItemData> lootItems;
    }

    [Serializable]
    public class LootItemData
    {
        public string itemId;
        public string itemName;
        public string rarity;
        public int quantity;
    }
}
