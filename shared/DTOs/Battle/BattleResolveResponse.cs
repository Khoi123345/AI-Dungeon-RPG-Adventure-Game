using System;
using System.Collections.Generic;
using GameShared.DTOs.Character;

namespace GameShared.DTOs.Battle
{
    /// <summary>
    /// Kết quả trận chiến được server tính toán.
    /// Client nhận dữ liệu này để hiển thị kết quả battle.
    /// </summary>
    [Serializable]
    public class BattleResolveResponse
    {
        public string battleId;
        public string encounterId;
        public bool isPlayerVictory;

        /// <summary>Sức mạnh tổng của player (Attack Total + Level Bonus).</summary>
        public double playerPower;

        /// <summary>Sức mạnh tổng của boss.</summary>
        public double bossPower;

        /// <summary>Battle Score quyết định thắng/thua (≥0 = Victory).</summary>
        public double battleScore;

        /// <summary>Các hiệu ứng may mắn đã kích hoạt: "Critical Hit", "Dodge", "Damage Bonus".</summary>
        public List<string> luckyEffects;

        /// <summary>Diễn biến mô tả trận đấu (1 phần tử tổng kết).</summary>
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
