using GameShared.Models;

namespace GameBackend.Core.Config
{
    /// <summary>
    /// Boss catalog và game constants data-driven.
    /// Tách từ static BossCatalog trong BossSpawnFunction cũ.
    /// </summary>
    public static class GameConstants
    {
        public static readonly List<Boss> BossCatalog = new()
        {
            new Boss { bossId = "boss_goblin_chief", name = "Goblin Chief", rarity = "Common", baseHp = 80, baseAttack = 10, baseDefense = 4, speed = 8, criticalRate = 0.05f, expReward = 30, goldReward = 40 },
            new Boss { bossId = "boss_skeleton_knight", name = "Skeleton Knight", rarity = "Common", baseHp = 100, baseAttack = 12, baseDefense = 6, speed = 6, criticalRate = 0.08f, expReward = 35, goldReward = 45 },
            new Boss { bossId = "boss_orc_warlord", name = "Orc Warlord", rarity = "Uncommon", baseHp = 140, baseAttack = 16, baseDefense = 7, speed = 7, criticalRate = 0.10f, expReward = 50, goldReward = 65 },
            new Boss { bossId = "boss_shadow_demon", name = "Shadow Demon", rarity = "Rare", baseHp = 200, baseAttack = 22, baseDefense = 9, speed = 12, criticalRate = 0.15f, expReward = 75, goldReward = 100 },
            new Boss { bossId = "boss_vampire_lord", name = "Vampire Lord", rarity = "Epic", baseHp = 300, baseAttack = 28, baseDefense = 12, speed = 14, criticalRate = 0.18f, expReward = 120, goldReward = 180 },
            new Boss { bossId = "boss_ancient_dragon", name = "Ancient Dragon", rarity = "Legendary", baseHp = 500, baseAttack = 35, baseDefense = 20, speed = 11, criticalRate = 0.20f, expReward = 250, goldReward = 400 }
        };

        private static readonly Random _random = new();

        public static Boss GetBossTemplateByRarity(string rarity)
        {
            var candidates = BossCatalog.Where(b => b.rarity == rarity).ToList();
            if (candidates.Count == 0) candidates = BossCatalog;
            return candidates[_random.Next(candidates.Count)];
        }
    }
}
