using System.Collections.Generic;
using GameShared.Models;
using GameShared.Config;

namespace GameBackend.Core.Config
{
    /// <summary>
    /// Uỷ quyền lại cho GameShared.Config.GameConstants dùng chung 100% dữ liệu với Client Unity.
    /// </summary>
    public static class GameConstants
    {
        public static List<Boss> BossCatalog => GameShared.Config.GameConstants.BossCatalog;
        public static List<Item> ItemCatalog => GameShared.Config.GameConstants.ItemCatalog;
        public static Dictionary<string, (int GoldMod, int ExpMod)> RarityMultipliers => GameShared.Config.GameConstants.RarityMultipliers;
        public const int MaxInventorySlots = GameShared.Config.GameConstants.MaxInventorySlots;

        public static string RollBossRarity() => GameShared.Config.GameConstants.RollBossRarity();
        public static int GetBossRarityLevelModifier(string rarity) => GameShared.Config.GameConstants.GetBossRarityLevelModifier(rarity);
        public static int CalculateBossLevel(int playerLevel, string rarity) => GameShared.Config.GameConstants.CalculateBossLevel(playerLevel, rarity);
        public static Boss GetBossTemplateByRarity(string rarity) => GameShared.Config.GameConstants.GetBossTemplateByRarity(rarity);
        public static Item? GetItemById(string itemId) => GameShared.Config.GameConstants.GetItemById(itemId);
        public static string RollItemRarity(string bossRarity) => GameShared.Config.GameConstants.RollItemRarity(bossRarity);
        public static Item? RollRandomItemByRarity(string itemRarity) => GameShared.Config.GameConstants.RollRandomItemByRarity(itemRarity);
        public static int CalculateGoldReward(int bossLevel, string bossRarity) => GameShared.Config.GameConstants.CalculateGoldReward(bossLevel, bossRarity);
        public static int CalculateExpReward(int bossLevel, string bossRarity) => GameShared.Config.GameConstants.CalculateExpReward(bossLevel, bossRarity);

        public const int LevelUpHpGrowth = GameShared.Config.GameConstants.LevelUpHpGrowth;
        public const int LevelUpMpGrowth = GameShared.Config.GameConstants.LevelUpMpGrowth;
        public const int LevelUpAttackGrowth = GameShared.Config.GameConstants.LevelUpAttackGrowth;
        public const int LevelUpDefenseGrowth = GameShared.Config.GameConstants.LevelUpDefenseGrowth;
        public const int BaseRequiredXpPerLevel = GameShared.Config.GameConstants.BaseRequiredXpPerLevel;
        public const double BossLevelScaleFactor = GameShared.Config.GameConstants.BossLevelScaleFactor;
        public const double RandomFactorRange = GameShared.Config.GameConstants.RandomFactorRange;
        public const double DodgeBonusRatio = GameShared.Config.GameConstants.DodgeBonusRatio;
        public const double DamageBonusMin = GameShared.Config.GameConstants.DamageBonusMin;
        public const double DamageBonusMax = GameShared.Config.GameConstants.DamageBonusMax;
        public const int ReviveWaitMinutes = GameShared.Config.GameConstants.ReviveWaitMinutes;
        public const double RevivalHpRatio = GameShared.Config.GameConstants.RevivalHpRatio;
    }
}
