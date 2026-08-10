using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameShared.Models;

namespace GameShared.Config
{
    /// <summary>
    /// Hằng số game và catalog dữ liệu tĩnh dùng chung cho cả Client (Unity) và Server (AWS Backend).
    /// </summary>
    public static class GameConstants
    {
        // =====================================================================
        // BOSS CATALOG
        // =====================================================================

        public static readonly List<Boss> BossCatalog = new()
        {
            // --- Chapter Bosses ---
            new Boss { bossId = "boss_goblin_king",     name = "Goblin King",      rarity = "Common",    baseHp = 80,  baseAttack = 10, baseDefense = 4,  speed = 8,  criticalRate = 0.05f, expReward = 30,  goldReward = 40  },
            new Boss { bossId = "boss_skeleton_knight", name = "Skeleton Knight",  rarity = "Common",    baseHp = 100, baseAttack = 12, baseDefense = 6,  speed = 6,  criticalRate = 0.08f, expReward = 35,  goldReward = 45  },
            new Boss { bossId = "boss_orc_warlord",     name = "Orc Warlord",      rarity = "Rare",      baseHp = 140, baseAttack = 16, baseDefense = 7,  speed = 7,  criticalRate = 0.10f, expReward = 50,  goldReward = 65  },
            new Boss { bossId = "boss_shadow_demon",    name = "Shadow Demon",     rarity = "Rare",      baseHp = 200, baseAttack = 22, baseDefense = 9,  speed = 12, criticalRate = 0.15f, expReward = 75,  goldReward = 100 },
            new Boss { bossId = "boss_vampire_lord",    name = "Vampire Lord",     rarity = "Epic",      baseHp = 300, baseAttack = 28, baseDefense = 12, speed = 14, criticalRate = 0.18f, expReward = 120, goldReward = 180 },
            new Boss { bossId = "boss_dragon_king",     name = "Dragon King",      rarity = "Legendary", baseHp = 500, baseAttack = 35, baseDefense = 20, speed = 11, criticalRate = 0.20f, expReward = 250, goldReward = 400 },
            new Boss { bossId = "boss_chaos_titan",     name = "Chaos Titan",      rarity = "Mythic",    baseHp = 900, baseAttack = 55, baseDefense = 35, speed = 15, criticalRate = 0.25f, expReward = 600, goldReward = 1000 },

            // --- Minor Monsters (Mobs) for Random Encounters ---
            new Boss { bossId = "mob_void_remnant",    name = "Void Remnant",     rarity = "Common",    baseHp = 30,  baseAttack = 5,  baseDefense = 1,  speed = 6,  criticalRate = 0.02f, expReward = 15, goldReward = 10  },
            new Boss { bossId = "mob_cave_spider",     name = "Cave Spider",      rarity = "Common",    baseHp = 25,  baseAttack = 4,  baseDefense = 1,  speed = 10, criticalRate = 0.03f, expReward = 8,  goldReward = 8   },
            new Boss { bossId = "mob_goblin_scout",    name = "Goblin Scout",     rarity = "Common",    baseHp = 35,  baseAttack = 5,  baseDefense = 2,  speed = 8,  criticalRate = 0.04f, expReward = 12, goldReward = 12  },
            new Boss { bossId = "mob_shadow_spirit",   name = "Shadow Spirit",    rarity = "Common",    baseHp = 45,  baseAttack = 7,  baseDefense = 2,  speed = 9,  criticalRate = 0.06f, expReward = 18, goldReward = 15  },
            new Boss { bossId = "mob_temple_golem",    name = "Temple Golem",     rarity = "Common",    baseHp = 60,  baseAttack = 6,  baseDefense = 5,  speed = 4,  criticalRate = 0.02f, expReward = 22, goldReward = 18  },
            new Boss { bossId = "mob_goblin_guard",    name = "Goblin Guard",     rarity = "Common",    baseHp = 50,  baseAttack = 8,  baseDefense = 4,  speed = 6,  criticalRate = 0.05f, expReward = 20, goldReward = 20  },
            
            // New Mobs
            new Boss { bossId = "mob_cave_bat",        name = "Cave Bat",         rarity = "Common",    baseHp = 18,  baseAttack = 6,  baseDefense = 0,  speed = 12, criticalRate = 0.05f, expReward = 10, goldReward = 5   },
            new Boss { bossId = "mob_rock_slime",      name = "Rock Slime",       rarity = "Common",    baseHp = 55,  baseAttack = 3,  baseDefense = 8,  speed = 3,  criticalRate = 0.01f, expReward = 15, goldReward = 10  },
            new Boss { bossId = "mob_abyssal_spirit",  name = "Abyssal Spirit",   rarity = "Common",    baseHp = 50,  baseAttack = 9,  baseDefense = 3,  speed = 8,  criticalRate = 0.08f, expReward = 25, goldReward = 22  },
            new Boss { bossId = "mob_drowned_sailor",  name = "Drowned Sailor",   rarity = "Common",    baseHp = 65,  baseAttack = 7,  baseDefense = 4,  speed = 5,  criticalRate = 0.04f, expReward = 20, goldReward = 18  },
            new Boss { bossId = "mob_mutated_crab",    name = "Mutated Crab",     rarity = "Common",    baseHp = 40,  baseAttack = 8,  baseDefense = 10, speed = 4,  criticalRate = 0.02f, expReward = 18, goldReward = 15  },

            // --- Chapter 3 Mobs ---
            new Boss { bossId = "mob_young_dragon",   name = "Young Dragon",     rarity = "Rare",      baseHp = 90,  baseAttack = 14, baseDefense = 6,  speed = 8,  criticalRate = 0.08f, expReward = 45, goldReward = 35  },
            new Boss { bossId = "mob_fire_lizard",    name = "Fire Lizard",      rarity = "Common",    baseHp = 60,  baseAttack = 10, baseDefense = 4,  speed = 7,  criticalRate = 0.05f, expReward = 30, goldReward = 25  },
            new Boss { bossId = "mob_fire_raptor",    name = "Fire Raptor",      rarity = "Rare",      baseHp = 100, baseAttack = 16, baseDefense = 5,  speed = 11, criticalRate = 0.10f, expReward = 55, goldReward = 45  },
            new Boss { bossId = "mob_adult_dragon",   name = "Adult Dragon",     rarity = "Epic",      baseHp = 180, baseAttack = 24, baseDefense = 12, speed = 9,  criticalRate = 0.12f, expReward = 100, goldReward = 90  },
        };


        // =====================================================================
        // ITEM CATALOG
        // =====================================================================

        public static readonly List<Item> ItemCatalog = new()
        {
            // --- Quest Items ---
            new Item { itemId = "item_ancient_key",     name = "Ancient Key",           rarity = "Common",    itemType = "Quest",      slotType = "",         attackBonus = 0,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = false, sellPrice = 0,    buyPrice = 0,    requiredLevel = 1,  description = "Chiếc chìa khóa cổ xưa dùng để mở cổng Đền thờ bị lãng quên.", effectJson = "" },
            new Item { itemId = "item_elemental_core",  name = "Elemental Core",        rarity = "Epic",      itemType = "Quest",      slotType = "",         attackBonus = 0,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = false, sellPrice = 0,    buyPrice = 0,    requiredLevel = 1,  description = "Mảnh vỡ Lõi Nguyên Tố đầu tiên chứa đựng sức mạnh kỳ bí.", effectJson = "" },
            new Item { itemId = "item_sea_compass",     name = "Sea Compass",           rarity = "Rare",      itemType = "Quest",      slotType = "",         attackBonus = 0,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = false, sellPrice = 0,    buyPrice = 0,    requiredLevel = 5,  description = "Hải đồ biển sâu dùng để chỉ đường lặn xuống Rãnh Sâu Vô Tận.", effectJson = "" },
            new Item { itemId = "item_void_crystal",    name = "Void Crystal",          rarity = "Epic",      itemType = "Quest",      slotType = "",         attackBonus = 0,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = false, sellPrice = 0,    buyPrice = 0,    requiredLevel = 10, description = "Pha lê hư không dùng để giải giải bẫy ma thuật và mở cổng Cung Điện San Hô.", effectJson = "" },
            new Item { itemId = "item_fire_core",       name = "Fire Core",             rarity = "Epic",      itemType = "Quest",      slotType = "",         attackBonus = 0,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = false, sellPrice = 0,    buyPrice = 0,    requiredLevel = 15, description = "Lõi lửa kháng nhiệt nhận được sau khi tiêu diệt Shadow Demon, mở đường sang Vùng Đất Hoang Tàn.", effectJson = "" },
            new Item { itemId = "item_obsidian_key",   name = "Obsidian Key",          rarity = "Rare",      itemType = "Quest",      slotType = "",         attackBonus = 0,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = false, sellPrice = 0,    buyPrice = 0,    requiredLevel = 18, description = "Chìa khóa Hắc Diệu Thạch dùng để trèo lên Đỉnh Núi Hắc Diệu Thạch.", effectJson = "" },
            new Item { itemId = "item_dragon_blood_key",name = "Dragon Blood Key",      rarity = "Legendary", itemType = "Quest",      slotType = "",         attackBonus = 0,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = false, sellPrice = 0,    buyPrice = 0,    requiredLevel = 20, description = "Chìa khóa Long Huyết dùng để mở cổng Tổ Rồng.", effectJson = "" },



            // --- Common ---
            new Item { itemId = "item_rusty_sword",      name = "Rusty Sword",           rarity = "Common",    itemType = "Weapon",     slotType = "MainHand", attackBonus = 3,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = false, sellPrice = 10,   buyPrice = 30,   requiredLevel = 1,  description = "Một thanh kiếm gỉ sét, vẫn còn có thể chiến đấu.", effectJson = "" },
            new Item { itemId = "item_leather_vest",     name = "Leather Vest",          rarity = "Common",    itemType = "Armor",      slotType = "Chest",    attackBonus = 0,  defenseBonus = 5,  hpBonus = 10, criticalBonus = 0f,    stackable = false, sellPrice = 10,   buyPrice = 30,   requiredLevel = 1,  description = "Áo giáp da thô sơ, bảo vệ cơ bản.", effectJson = "" },
            new Item { itemId = "item_wooden_ring",      name = "Wooden Ring",           rarity = "Common",    itemType = "Accessory",  slotType = "Ring",     attackBonus = 1,  defenseBonus = 1,  hpBonus = 5,  criticalBonus = 0f,    stackable = false, sellPrice = 5,    buyPrice = 15,   requiredLevel = 1,  description = "Chiếc nhẫn gỗ được khắc phù văn đơn giản.", effectJson = "" },
            new Item { itemId = "item_health_potion_s",  name = "Small Health Potion",   rarity = "Common",    itemType = "Consumable", slotType = "",         attackBonus = 0,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = true,  sellPrice = 5,    buyPrice = 20,   requiredLevel = 1,  description = "Hồi phục 50 HP ngay lập tức.", effectJson = "{\"hp\": 50}" },

            // --- Rare ---
            new Item { itemId = "item_steel_dagger",     name = "Steel Dagger",          rarity = "Rare",      itemType = "Weapon",     slotType = "MainHand", attackBonus = 6,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0.02f, stackable = false, sellPrice = 50,   buyPrice = 150,  requiredLevel = 5,  description = "Dao găm thép sắc bén, tốc độ đánh nhanh.", effectJson = "" },
            new Item { itemId = "item_steel_blade",      name = "Steel Blade",          rarity = "Rare",      itemType = "Weapon",     slotType = "MainHand", attackBonus = 6,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0.02f, stackable = false, sellPrice = 50,   buyPrice = 150,  requiredLevel = 5,  description = "Dao găm thép sắc bén, tốc độ đánh nhanh.", effectJson = "" },
            new Item { itemId = "item_iron_shield",      name = "Iron Shield",           rarity = "Rare",      itemType = "Armor",      slotType = "Chest",    attackBonus = 0,  defenseBonus = 10, hpBonus = 15, criticalBonus = 0f,    stackable = false, sellPrice = 50,   buyPrice = 150,  requiredLevel = 5,  description = "Khiên sắt cứng cáp, chắc chắn.", effectJson = "" },
            new Item { itemId = "item_silver_amulet",    name = "Silver Amulet",         rarity = "Rare",      itemType = "Accessory",  slotType = "Neck",     attackBonus = 3,  defenseBonus = 3,  hpBonus = 20, criticalBonus = 0.01f, stackable = false, sellPrice = 60,   buyPrice = 180,  requiredLevel = 5,  description = "Bùa hộ mệnh bạc mang lại may mắn.", effectJson = "" },
            new Item { itemId = "item_health_potion_m",  name = "Medium Health Potion",  rarity = "Rare",      itemType = "Consumable", slotType = "",         attackBonus = 0,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = true,  sellPrice = 20,   buyPrice = 70,   requiredLevel = 5,  description = "Hồi phục 150 HP ngay lập tức.", effectJson = "{\"hp\": 150}" },

            // --- Epic ---
            new Item { itemId = "item_shadow_blade",     name = "Shadow Blade",          rarity = "Epic",      itemType = "Weapon",     slotType = "MainHand", attackBonus = 12, defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0.05f, stackable = false, sellPrice = 200,  buyPrice = 600,  requiredLevel = 15, description = "Lưỡi kiếm được rèn từ bóng tối, sắc bén tuyệt đối.", effectJson = "" },
            new Item { itemId = "item_dragon_scale",     name = "Dragon Scale Plate",    rarity = "Epic",      itemType = "Armor",      slotType = "Chest",    attackBonus = 0,  defenseBonus = 15, hpBonus = 40, criticalBonus = 0f,    stackable = false, sellPrice = 200,  buyPrice = 600,  requiredLevel = 15, description = "Giáp chế tác từ vảy rồng, cực kỳ bền chắc.", effectJson = "" },
            new Item { itemId = "item_void_ring",        name = "Void Ring",             rarity = "Epic",      itemType = "Accessory",  slotType = "Ring",     attackBonus = 8,  defenseBonus = 5,  hpBonus = 30, criticalBonus = 0.03f, stackable = false, sellPrice = 220,  buyPrice = 650,  requiredLevel = 15, description = "Chiếc nhẫn thấm đẫm năng lượng hư không.", effectJson = "" },
            new Item { itemId = "item_elixir",           name = "Battle Elixir",         rarity = "Epic",      itemType = "Consumable", slotType = "",         attackBonus = 0,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = true,  sellPrice = 80,   buyPrice = 250,  requiredLevel = 10, description = "Hồi phục 400 HP ngay lập tức.", effectJson = "{\"hp\": 400}" },

            // --- Legendary ---
            new Item { itemId = "item_excalibur",        name = "Excalibur",             rarity = "Legendary", itemType = "Weapon",     slotType = "MainHand", attackBonus = 25, defenseBonus = 0,  hpBonus = 20, criticalBonus = 0.10f, stackable = false, sellPrice = 1000, buyPrice = 5000, requiredLevel = 30, description = "Thanh kiếm thần thánh của vua Arthur, sức mạnh vô song.", effectJson = "" },
            new Item { itemId = "item_aegis",            name = "Aegis of the Ancients", rarity = "Legendary", itemType = "Armor",      slotType = "Chest",    attackBonus = 0,  defenseBonus = 30, hpBonus = 80, criticalBonus = 0f,    stackable = false, sellPrice = 1000, buyPrice = 5000, requiredLevel = 30, description = "Khiên huyền thoại được các thần linh ban phước.", effectJson = "" },
            new Item { itemId = "item_ring_of_gods",     name = "Ring of the Gods",      rarity = "Legendary", itemType = "Accessory",  slotType = "Ring",     attackBonus = 15, defenseBonus = 15, hpBonus = 60, criticalBonus = 0.08f, stackable = false, sellPrice = 1200, buyPrice = 6000, requiredLevel = 30, description = "Chiếc nhẫn do chính tay các vị thần tạo ra.", effectJson = "" },
            new Item { itemId = "item_divine_elixir",    name = "Divine Elixir",         rarity = "Legendary", itemType = "Consumable", slotType = "",         attackBonus = 0,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = true,  sellPrice = 300,  buyPrice = 1200, requiredLevel = 20, description = "Hồi phục toàn bộ HP ngay lập tức.", effectJson = "{\"hp_full\": true}" },
        };

        // =====================================================================
        // RARITY MULTIPLIERS
        // =====================================================================

        public static readonly Dictionary<string, (int GoldMod, int ExpMod)> RarityMultipliers = new()
        {
            { "Minor",     (GoldMod: 2,   ExpMod: 10)  },
            { "Common",    (GoldMod: 10,  ExpMod: 15)  },
            { "Rare",      (GoldMod: 20,  ExpMod: 30)  },
            { "Epic",      (GoldMod: 40,  ExpMod: 60)  },
            { "Legendary", (GoldMod: 80,  ExpMod: 120) },
            { "Mythic",    (GoldMod: 200, ExpMod: 300) },
        };

        // =====================================================================
        // LOOT DROP TABLE
        // =====================================================================

        private static readonly Dictionary<string, (int Common, int Rare, int Epic, int Legendary)> LootDropTable = new()
        {
            { "Common",    (Common: 80, Rare: 20, Epic: 0,  Legendary: 0)  },
            { "Rare",      (Common: 50, Rare: 39, Epic: 10, Legendary: 1)  },
            { "Epic",      (Common: 30, Rare: 42, Epic: 25, Legendary: 3)  },
            { "Legendary", (Common: 18, Rare: 29, Epic: 44, Legendary: 9)  },
            { "Mythic",    (Common: 10, Rare: 20, Epic: 50, Legendary: 20) },
        };

        public const int MaxInventorySlots = 100;

        public static readonly (string Rarity, double CumulativeWeight)[] BossRarityWeights =
        {
            ("Common",    0.60),
            ("Rare",      0.85),
            ("Epic",      0.95),
            ("Legendary", 0.99),
            ("Mythic",    1.00),
        };

        public static readonly Dictionary<string, int> BossRarityLevelModifier = new()
        {
            { "Common",    0  },
            { "Rare",      5  },
            { "Epic",      12 },
            { "Legendary", 25 },
            { "Mythic",    50 },
        };

        public const int BossLevelRandomMin = -3;
        public const int BossLevelRandomMax = 3;

        public const int LevelUpHpGrowth      = 12;
        public const int LevelUpMpGrowth      = 5;
        public const int LevelUpAttackGrowth  = 3;
        public const int LevelUpDefenseGrowth = 2;
        public const int BaseRequiredXpPerLevel = 100;

        public const double BossLevelScaleFactor = 0.1;
        public const double RandomFactorRange = 0.1;
        public const double DodgeBonusRatio = 0.2;
        public const double DamageBonusMin = 0.1;
        public const double DamageBonusMax = 0.3;

        public const int ReviveWaitMinutes = 5;
        public const double RevivalHpRatio = 0.5;

        // GOLD ECONOMY
        public const int StoryCostPerTurn = 5;          // Mỗi lượt AI kể chuyện tốn 5 Gold
        public const int InstantReviveCost = 50;         // Hồi sinh bằng Vàng tốn 50 Gold

        private static readonly Random _random = new();

        public static string RollBossRarity()
        {
            double roll = _random.NextDouble();
            foreach (var (rarity, cumulativeWeight) in BossRarityWeights)
            {
                if (roll <= cumulativeWeight) return rarity;
            }
            return "Common";
        }

        public static int GetBossRarityLevelModifier(string rarity)
        {
            return BossRarityLevelModifier.TryGetValue(rarity, out int mod) ? mod : 0;
        }

        public static int CalculateBossLevel(int playerLevel, string rarity, string? bossId = null)
        {
            var cleanId = (bossId ?? "").ToLowerInvariant();

            // 1. Chapter Bosses: Luôn tạo chênh lệch cấp độ đáng kể (Level Gap)
            if (cleanId.Contains("goblin_king") || cleanId.Contains("boss_goblin"))
            {
                int gap = Math.Max(2, playerLevel / 3 + 2);
                return Math.Max(5, playerLevel + _random.Next(gap, gap + 4));
            }
            if (cleanId.Contains("shadow_demon") || cleanId.Contains("boss_shadow"))
            {
                int gap = Math.Max(3, playerLevel / 3 + 3);
                return Math.Max(12, playerLevel + _random.Next(gap, gap + 4));
            }
            if (cleanId.Contains("dragon_king") || cleanId.Contains("boss_dragon"))
            {
                int gap = Math.Max(5, playerLevel / 3 + 5);
                return Math.Max(25, playerLevel + _random.Next(gap, gap + 5));
            }

            // 2. Dynamic Player Level Scaling cho Mobs:
            // Cấp độ quái vật dao động ngẫu nhiên quanh Level người chơi (-1, 0, +1, +2 level) để tạo sự đa dạng
            int randomLevelOffset = _random.Next(-1, 3); // [-1, 0, 1, 2]
            int rarityMod = GetBossRarityLevelModifier(rarity); // Common=0, Rare=1, Epic=2
            return Math.Max(1, playerLevel + rarityMod + randomLevelOffset);
        }


        public static Boss GetBossTemplateByRarity(string rarity)
        {
            var candidates = BossCatalog.Where(b => b.rarity == rarity).ToList();
            if (candidates.Count == 0) candidates = BossCatalog;
            return candidates[_random.Next(candidates.Count)];
        }

        public static Item? GetItemById(string itemId)
            => ItemCatalog.FirstOrDefault(i => i.itemId == itemId);

        public static string RollItemRarity(string bossRarity)
        {
            if (!LootDropTable.TryGetValue(bossRarity, out var table))
                table = LootDropTable["Common"];

            int roll = _random.Next(0, 100);
            if (roll < table.Common)    return "Common";
            if (roll < table.Common + table.Rare)   return "Rare";
            if (roll < table.Common + table.Rare + table.Epic) return "Epic";
            return "Legendary";
        }

        public static Item? RollRandomItemByRarity(string itemRarity)
        {
            var candidates = ItemCatalog
                .Where(i => i.rarity == itemRarity && i.itemType != "Consumable" && i.itemType != "Quest")
                .ToList();
            if (candidates.Count == 0) return null;
            return candidates[_random.Next(candidates.Count)];
        }

        public static int CalculateGoldReward(int bossLevel, string bossRarity)
        {
            if (!RarityMultipliers.TryGetValue(bossRarity, out var mods))
                mods = RarityMultipliers["Common"];
            if (bossRarity.Equals("Minor", StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(5, bossLevel * mods.GoldMod + _random.Next(2, 6));
            }
            return bossLevel * mods.GoldMod + _random.Next(10, 51);
        }

        public static int CalculateExpReward(int bossLevel, string bossRarity, int playerLevel = 1)
        {
            if (!RarityMultipliers.TryGetValue(bossRarity, out var mods))
                mods = RarityMultipliers["Common"];

            int baseExp = bossLevel * mods.ExpMod;

            // Higher-Level Boss Victory EXP Bonus Multiplier:
            if (bossLevel > playerLevel)
            {
                int gap = bossLevel - playerLevel;
                double bonusMultiplier = 1.0 + (gap * 0.5); // e.g. gap=1 -> 1.5x, gap=2 -> 2.0x, gap=4 -> 3.0x
                baseExp = (int)Math.Round(baseExp * bonusMultiplier);
            }

            return baseExp;
        }

        /// <summary>Scale stat theo level: baseStat × (1 + 0.08 × level)</summary>
        public static int ScaleStat(int baseStat, int level)
        {
            return Math.Max(1, (int)Math.Round(baseStat * (1.0 + 0.08 * level)));
        }
    }
}
