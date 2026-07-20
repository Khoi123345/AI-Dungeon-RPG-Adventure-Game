using GameShared.Models;

namespace GameBackend.Core.Config
{
    /// <summary>
    /// Hằng số game và catalog dữ liệu tĩnh.
    /// Bao gồm: Boss Catalog, Item Catalog, Loot Drop Table, Rarity Multipliers,
    /// Battle Formula, Level-Up Growth, Death & Revival config.
    /// </summary>
    public static class GameConstants
    {
        // =====================================================================
        // BOSS CATALOG
        // =====================================================================

        public static readonly List<Boss> BossCatalog = new()
        {
            new Boss { bossId = "boss_goblin_chief",    name = "Goblin Chief",     rarity = "Common",    baseHp = 80,  baseAttack = 10, baseDefense = 4,  speed = 8,  criticalRate = 0.05f, expReward = 30,  goldReward = 40  },
            new Boss { bossId = "boss_skeleton_knight", name = "Skeleton Knight",  rarity = "Common",    baseHp = 100, baseAttack = 12, baseDefense = 6,  speed = 6,  criticalRate = 0.08f, expReward = 35,  goldReward = 45  },
            new Boss { bossId = "boss_orc_warlord",     name = "Orc Warlord",      rarity = "Rare",      baseHp = 140, baseAttack = 16, baseDefense = 7,  speed = 7,  criticalRate = 0.10f, expReward = 50,  goldReward = 65  },
            new Boss { bossId = "boss_shadow_demon",    name = "Shadow Demon",     rarity = "Rare",      baseHp = 200, baseAttack = 22, baseDefense = 9,  speed = 12, criticalRate = 0.15f, expReward = 75,  goldReward = 100 },
            new Boss { bossId = "boss_vampire_lord",    name = "Vampire Lord",     rarity = "Epic",      baseHp = 300, baseAttack = 28, baseDefense = 12, speed = 14, criticalRate = 0.18f, expReward = 120, goldReward = 180 },
            new Boss { bossId = "boss_ancient_dragon",  name = "Ancient Dragon",   rarity = "Legendary", baseHp = 500, baseAttack = 35, baseDefense = 20, speed = 11, criticalRate = 0.20f, expReward = 250, goldReward = 400 },
            new Boss { bossId = "boss_chaos_titan",     name = "Chaos Titan",      rarity = "Mythic",    baseHp = 900, baseAttack = 55, baseDefense = 35, speed = 15, criticalRate = 0.25f, expReward = 600, goldReward = 1000 },
        };

        // =====================================================================
        // ITEM CATALOG (Hardcode — không cần DynamoDB table riêng)
        // =====================================================================

        public static readonly List<Item> ItemCatalog = new()
        {
            // --- Common ---
            new Item { itemId = "item_rusty_sword",      name = "Rusty Sword",           rarity = "Common",    itemType = "Weapon",     slotType = "MainHand", attackBonus = 3,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = false, sellPrice = 10,   buyPrice = 30,   requiredLevel = 1,  description = "Một thanh kiếm gỉ sét, vẫn còn có thể chiến đấu.", effectJson = "" },
            new Item { itemId = "item_leather_vest",     name = "Leather Vest",          rarity = "Common",    itemType = "Armor",      slotType = "Chest",    attackBonus = 0,  defenseBonus = 5,  hpBonus = 10, criticalBonus = 0f,    stackable = false, sellPrice = 10,   buyPrice = 30,   requiredLevel = 1,  description = "Áo giáp da thô sơ, bảo vệ cơ bản.", effectJson = "" },
            new Item { itemId = "item_wooden_ring",      name = "Wooden Ring",           rarity = "Common",    itemType = "Accessory",  slotType = "Ring",     attackBonus = 1,  defenseBonus = 1,  hpBonus = 5,  criticalBonus = 0f,    stackable = false, sellPrice = 5,    buyPrice = 15,   requiredLevel = 1,  description = "Chiếc nhẫn gỗ được khắc phù văn đơn giản.", effectJson = "" },
            new Item { itemId = "item_health_potion_s",  name = "Small Health Potion",   rarity = "Common",    itemType = "Consumable", slotType = "",         attackBonus = 0,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0f,    stackable = true,  sellPrice = 5,    buyPrice = 20,   requiredLevel = 1,  description = "Hồi phục 50 HP ngay lập tức.", effectJson = "{\"hp\": 50}" },

            // --- Rare ---
            new Item { itemId = "item_steel_dagger",     name = "Steel Dagger",          rarity = "Rare",      itemType = "Weapon",     slotType = "MainHand", attackBonus = 6,  defenseBonus = 0,  hpBonus = 0,  criticalBonus = 0.02f, stackable = false, sellPrice = 50,   buyPrice = 150,  requiredLevel = 5,  description = "Dao găm thép sắc bén, tốc độ đánh nhanh.", effectJson = "" },
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
        // RARITY MULTIPLIERS — Dùng cho công thức Gold và XP (mục 5.1 logic doc)
        // Gold = BossLevel × GoldMod + Random(10, 50)
        // XP   = BossLevel × ExpMod
        // =====================================================================

        public static readonly Dictionary<string, (int GoldMod, int ExpMod)> RarityMultipliers = new()
        {
            { "Common",    (GoldMod: 10,  ExpMod: 15)  },
            { "Rare",      (GoldMod: 20,  ExpMod: 30)  },
            { "Epic",      (GoldMod: 40,  ExpMod: 60)  },
            { "Legendary", (GoldMod: 80,  ExpMod: 120) },
            { "Mythic",    (GoldMod: 200, ExpMod: 300) },
        };

        // =====================================================================
        // LOOT DROP TABLE — Xác suất item rarity theo boss rarity (mục 5.2 logic doc)
        // Key: bossRarity, Value: (Common%, Rare%, Epic%, Legendary%)
        // Tổng = 100% với mỗi tier boss
        // =====================================================================

        private static readonly Dictionary<string, (int Common, int Rare, int Epic, int Legendary)> LootDropTable = new()
        {
            { "Common",    (Common: 80, Rare: 20, Epic: 0,  Legendary: 0)  },
            { "Rare",      (Common: 50, Rare: 39, Epic: 10, Legendary: 1)  },
            { "Epic",      (Common: 30, Rare: 42, Epic: 25, Legendary: 3)  },
            { "Legendary", (Common: 18, Rare: 29, Epic: 44, Legendary: 9)  },
            { "Mythic",    (Common: 10, Rare: 20, Epic: 50, Legendary: 20) },  // Tổng = 100% (sửa từ 30→20 theo prompt)
        };

        // =====================================================================
        // INVENTORY CONSTANTS
        // =====================================================================

        /// <summary>Số ô kho đồ tối đa mỗi nhân vật (mục 2.5 logic doc).</summary>
        public const int MaxInventorySlots = 100;

        // =====================================================================
        // BOSS RARITY WEIGHTS (Mục 3 logic doc)
        // Common 60%, Rare 25%, Epic 10%, Legendary 4%, Mythic 1%
        // =====================================================================

        public static readonly (string Rarity, double CumulativeWeight)[] BossRarityWeights =
        {
            ("Common",    0.60),
            ("Rare",      0.85),  // 0.60 + 0.25
            ("Epic",      0.95),  // 0.85 + 0.10
            ("Legendary", 0.99),  // 0.95 + 0.04
            ("Mythic",    1.00),  // 0.99 + 0.01
        };

        // =====================================================================
        // BOSS LEVEL MODIFIERS (Mục 3 logic doc)
        // BossLevel = PlayerLevel + RarityModifier + Random(-3, 3)
        // =====================================================================

        public static readonly Dictionary<string, int> BossRarityLevelModifier = new()
        {
            { "Common",    0  },
            { "Rare",      5  },
            { "Epic",      12 },
            { "Legendary", 25 },
            { "Mythic",    50 },
        };

        public const int BossLevelRandomMin = -3;
        public const int BossLevelRandomMax = 3;  // inclusive → Random(min, max+1)

        // =====================================================================
        // LEVEL UP GROWTH CONSTANTS (Mục 1 logic doc)
        // =====================================================================

        public const int LevelUpHpGrowth      = 12;
        public const int LevelUpMpGrowth      = 5;
        public const int LevelUpAttackGrowth  = 3;
        public const int LevelUpDefenseGrowth = 2;
        public const int BaseRequiredXpPerLevel = 100;  // RequiredXP = level * 100

        // =====================================================================
        // BATTLE FORMULA CONSTANTS (Mục 4 logic doc)
        // =====================================================================

        /// <summary>Boss Power scaling: BaseAttack × (1 + Level × BossLevelScaleFactor)</summary>
        public const double BossLevelScaleFactor = 0.1;

        /// <summary>Random Factor range: ±RandomFactorRange × PlayerPower</summary>
        public const double RandomFactorRange = 0.1;

        /// <summary>Dodge Lucky Effect: +DodgeBonusRatio × BossPower vào Battle Score</summary>
        public const double DodgeBonusRatio = 0.2;

        /// <summary>Damage Bonus Lucky Effect: Random(min, max) × PlayerPower</summary>
        public const double DamageBonusMin = 0.1;
        public const double DamageBonusMax = 0.3;

        // =====================================================================
        // DEATH & REVIVAL CONSTANTS (Mục 6 logic doc)
        // =====================================================================

        /// <summary>Thời gian chờ hồi sinh (phút).</summary>
        public const int ReviveWaitMinutes = 5;

        /// <summary>Tỷ lệ HP hồi phục khi revive (0.5 = 50% MaxHP).</summary>
        public const double RevivalHpRatio = 0.5;

        // =====================================================================
        // HELPER METHODS
        // =====================================================================

        private static readonly Random _random = new();

        /// <summary>
        /// Roll ngẫu nhiên rarity cho Boss theo weighted random (Mục 3).
        /// Common 60%, Rare 25%, Epic 10%, Legendary 4%, Mythic 1%.
        /// </summary>
        public static string RollBossRarity()
        {
            double roll = _random.NextDouble();
            foreach (var (rarity, cumulativeWeight) in BossRarityWeights)
            {
                if (roll <= cumulativeWeight) return rarity;
            }
            return "Common"; // fallback
        }

        /// <summary>
        /// Lấy RarityModifier cho công thức Boss Level (Mục 3).
        /// </summary>
        public static int GetBossRarityLevelModifier(string rarity)
        {
            return BossRarityLevelModifier.TryGetValue(rarity, out int mod) ? mod : 0;
        }

        /// <summary>
        /// Tính Boss Level theo công thức: PlayerLevel + RarityModifier + Random(-3, 3).
        /// Clamp kết quả >= 1.
        /// </summary>
        public static int CalculateBossLevel(int playerLevel, string rarity)
        {
            int rarityMod = GetBossRarityLevelModifier(rarity);
            int randomMod = _random.Next(BossLevelRandomMin, BossLevelRandomMax + 1);
            return Math.Max(1, playerLevel + rarityMod + randomMod);
        }

        public static Boss GetBossTemplateByRarity(string rarity)
        {
            var candidates = BossCatalog.Where(b => b.rarity == rarity).ToList();
            if (candidates.Count == 0) candidates = BossCatalog;
            return candidates[_random.Next(candidates.Count)];
        }

        /// <summary>Lấy thông tin item từ catalog theo itemId. Trả null nếu không tìm thấy.</summary>
        public static Item? GetItemById(string itemId)
            => ItemCatalog.FirstOrDefault(i => i.itemId == itemId);

        /// <summary>
        /// Roll ngẫu nhiên độ hiếm item dựa theo rarity của boss (Weighted Random).
        /// Trả về: "Common" | "Rare" | "Epic" | "Legendary"
        /// </summary>
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

        /// <summary>
        /// Chọn ngẫu nhiên một item từ catalog có đúng rarity yêu cầu.
        /// Trả null nếu không có item nào trong rarity đó (không nên xảy ra).
        /// </summary>
        public static Item? RollRandomItemByRarity(string itemRarity)
        {
            // Không drop Consumable từ boss loot — chỉ drop Equipment
            var candidates = ItemCatalog
                .Where(i => i.rarity == itemRarity && i.itemType != "Consumable")
                .ToList();
            if (candidates.Count == 0) return null;
            return candidates[_random.Next(candidates.Count)];
        }

        /// <summary>
        /// Tính lượng Gold thưởng theo mục 5.1 logic doc:
        /// Gold = BossLevel × GoldMod + Random(10, 50)
        /// </summary>
        public static int CalculateGoldReward(int bossLevel, string bossRarity)
        {
            if (!RarityMultipliers.TryGetValue(bossRarity, out var mods))
                mods = RarityMultipliers["Common"];
            return bossLevel * mods.GoldMod + _random.Next(10, 51);
        }

        /// <summary>
        /// Tính lượng XP thưởng theo mục 5.1 logic doc:
        /// XP = BossLevel × ExpMod
        /// </summary>
        public static int CalculateExpReward(int bossLevel, string bossRarity)
        {
            if (!RarityMultipliers.TryGetValue(bossRarity, out var mods))
                mods = RarityMultipliers["Common"];
            return bossLevel * mods.ExpMod;
        }
    }
}
