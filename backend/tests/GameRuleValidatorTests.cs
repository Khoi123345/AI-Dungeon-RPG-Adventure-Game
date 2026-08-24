using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameBackend.Core.AIStory.Services;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Validation;
using GameShared.DTOs.Story;
using GameShared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class GameRuleValidatorTests
{
    [Fact]
    public async Task ValidateAndSanitizeAsync_Should_Reject_Invalid_Boss_And_Invalid_Item()
    {
        var validator = BuildValidator(new FakeContentService(
            validBosses: Array.Empty<string>(),
            validItems: new[] { "ancient_key" },
            validLocations: new[] { "dragon_cave", "ancient_cave_depths" },
            validQuests: Array.Empty<string>()));

        var session = CreateSession(location: "dragon_cave");
        var character = CreateCharacter(location: "dragon_cave");

        var aiResponse = new StoryAiResponse
        {
            TriggerBattle = true,
            BossId = "dragon_001",
            CurrentLocation = "dragon_cave",
            InventoryChanges = new List<StoryAiInventoryChange>
            {
                new() { ItemId = "legendary_sword", QuantityDelta = 1 },
                new() { ItemId = "ancient_key", QuantityDelta = 1 }
            },
            CharacterDelta = new StoryAiCharacterDelta()
        };

        var sanitized = await validator.ValidateAndSanitizeAsync(session, character, aiResponse);

        Assert.False(sanitized.TriggerBattle);
        Assert.Null(sanitized.BossId);
        Assert.Single(sanitized.InventoryChanges);
        Assert.Equal("ancient_key", sanitized.InventoryChanges[0].ItemId);
    }

    [Fact]
    public async Task ValidateAndSanitizeAsync_Should_Reject_All_Ai_Character_Deltas()
    {
        var validator = BuildValidator(new FakeContentService(
            validBosses: new[] { "dragon_001" },
            validItems: Array.Empty<string>(),
            validLocations: new[] { "ancient_cave_depths" },
            validQuests: Array.Empty<string>()));

        var session = CreateSession(location: "ancient_cave_depths");
        var character = CreateCharacter(location: "ancient_cave_depths");

        var aiResponse = new StoryAiResponse
        {
            CharacterDelta = new StoryAiCharacterDelta
            {
                HpDelta = 9999,
                GoldDelta = 100000,
                ExpDelta = 99999,
                MpDelta = 9999,
                Status = "GodMode"
            },
            NarrativeText = "Dragon chết."
        };

        var sanitized = await validator.ValidateAndSanitizeAsync(session, character, aiResponse);

        Assert.Equal(0, sanitized.CharacterDelta.HpDelta);
        Assert.Equal(0, sanitized.CharacterDelta.GoldDelta);
        Assert.Equal(0, sanitized.CharacterDelta.ExpDelta);
        Assert.Equal(0, sanitized.CharacterDelta.MpDelta);
        Assert.Equal("Alive", sanitized.CharacterDelta.Status);
        Assert.DoesNotContain("Dragon chết", sanitized.NarrativeText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAndSanitizeAsync_Should_Block_Goblin_Hideout_Label_When_Elemental_Core_Is_Missing()
    {
        var validator = BuildValidator(new FakeContentService(
            Array.Empty<string>(), Array.Empty<string>(),
            new[] { "forgotten_temple", "goblin_hideout" }, Array.Empty<string>()));
        var session = CreateSession("forgotten_temple");
        var character = CreateCharacter("forgotten_temple");
        var response = new StoryAiResponse
        {
            CurrentLocation = "forgotten_temple",
            CurrentNodeId = "explore_temple",
            NarrativeText = "Cánh cổng sào huyệt hiện ra.",
            Choices = new List<StoryChoiceOption>
            {
                new() { label = "Tiến vào Sào Huyệt Goblin", description = "Đi qua cánh cổng", nextNodeId = "next_path" },
                new() { label = "Khám phá thêm Đền Thờ", description = "Tiếp tục tìm kiếm", nextNodeId = "explore_temple" }
            }
        };

        var sanitized = await validator.ValidateAndSanitizeAsync(session, character, response);

        Assert.Single(sanitized.Choices);
        Assert.Equal("Khám phá thêm Đền Thờ", sanitized.Choices[0].label);
    }

    [Fact]
    public async Task ValidateAndSanitizeAsync_Should_Require_Positive_Elemental_Core_Quantity()
    {
        var content = new FakeContentService(Array.Empty<string>(), Array.Empty<string>(),
            new[] { "forgotten_temple", "goblin_hideout" }, Array.Empty<string>());
        var zeroQuantity = new Inventory { itemId = "item_elemental_core", quantity = 0, characterId = "char-1" };
        var validator = BuildValidator(content, new[] { zeroQuantity });
        var response = new StoryAiResponse
        {
            CurrentLocation = "forgotten_temple",
            CurrentNodeId = "explore_temple",
            Choices = new List<StoryChoiceOption>
            {
                new() { label = "Tiến vào Sào Huyệt Goblin", nextNodeId = "next_path" }
            }
        };

        var blocked = await validator.ValidateAndSanitizeAsync(
            CreateSession("forgotten_temple"), CreateCharacter("forgotten_temple"), response);
        Assert.Empty(blocked.Choices);

        var positiveQuantity = new Inventory { itemId = "item_elemental_core", quantity = 1, characterId = "char-1" };
        validator = BuildValidator(content, new[] { positiveQuantity });
        response = new StoryAiResponse
        {
            CurrentLocation = "forgotten_temple",
            CurrentNodeId = "explore_temple",
            Choices = new List<StoryChoiceOption>
            {
                new() { label = "Tiến vào Sào Huyệt Goblin", nextNodeId = "next_path" }
            }
        };
        var allowed = await validator.ValidateAndSanitizeAsync(
            CreateSession("forgotten_temple"), CreateCharacter("forgotten_temple"), response);
        Assert.Single(allowed.Choices);
    }

    [Fact]
    public async Task ValidateAndSanitizeAsync_Should_Keep_Drowned_Sailor_Battle_When_Only_Environment_Dissolves()
    {
        var validator = BuildValidator(new FakeContentService(
            Array.Empty<string>(), Array.Empty<string>(), new[] { "sunken_shipwreck" }, Array.Empty<string>()));
        var session = CreateSession("sunken_shipwreck");
        session.currentChapterId = "chapter_2";
        var character = CreateCharacter("sunken_shipwreck");
        var response = new StoryAiResponse
        {
            TriggerBattle = true,
            BossId = "mob_drowned_sailor",
            BossName = "Drowned Sailor",
            CurrentLocation = "sunken_shipwreck",
            CurrentNodeId = "shipwreck_deck",
            NarrativeText = "Bóng tối trên boong tàu tan biến khi Thủy Thủ Chết Đuối bước ra và lao về phía bạn."
        };

        var sanitized = await validator.ValidateAndSanitizeAsync(session, character, response);

        Assert.True(sanitized.TriggerBattle);
        Assert.Equal("mob_drowned_sailor", sanitized.BossId);
    }

    [Fact]
    public async Task ValidateAndSanitizeAsync_Should_Reject_Ai_Authored_Fire_Core()
    {
        var validator = BuildValidator(new FakeContentService(
            Array.Empty<string>(), Array.Empty<string>(), new[] { "coral_palace" }, Array.Empty<string>()));
        var session = CreateSession("coral_palace");
        session.currentChapterId = "chapter_2";
        var response = new StoryAiResponse
        {
            CurrentLocation = "coral_palace",
            CurrentNodeId = "coral_palace",
            InventoryChanges = new List<StoryAiInventoryChange>
            {
                new() { ItemId = "item_fire_core", QuantityDelta = 1 }
            }
        };

        var sanitized = await validator.ValidateAndSanitizeAsync(session, CreateCharacter("coral_palace"), response);

        Assert.Empty(sanitized.InventoryChanges);
    }

    [Fact]
    public void Common_Mob_Loot_Should_Never_Exceed_Common_Rarity()
    {
        var cap = GameShared.Config.GameConstants.GetMaxRarityCap("mob_goblin_guard", "Common");
        Assert.Equal("Common", cap);
        Assert.True(GameShared.Config.GameConstants.IsRarityAtOrBelow("Common", cap));
        Assert.False(GameShared.Config.GameConstants.IsRarityAtOrBelow("Rare", cap));
    }

    [Theory]
    [InlineData("mob_shadow_spirit")]
    [InlineData("mob_void_remnant")]
    [InlineData("boss_goblin_king")]
    [InlineData("")]
    public void CoralPalace_NonShadowDemon_Should_Never_Grant_FireCore(string enemyId)
    {
        Assert.Null(GameBackend.Core.Services.BattleService.GetKeyItemReward("coral_palace", enemyId));
    }

    [Fact]
    public void CoralPalace_ShadowDemon_Should_Grant_FireCore()
    {
        Assert.Equal(
            "item_fire_core",
            GameBackend.Core.Services.BattleService.GetKeyItemReward("coral_palace", "boss_shadow_demon"));
    }

    [Theory]
    [InlineData("ancient_cave", "mob_cave_spider", "item_ancient_key")]
    [InlineData("forgotten_temple", "mob_temple_golem", "item_elemental_core")]
    [InlineData("sunken_shipwreck", "mob_drowned_sailor", "item_sea_compass")]
    [InlineData("abyssal_trench", "mob_void_remnant", "item_void_crystal")]
    [InlineData("coral_palace", "boss_shadow_demon", "item_fire_core")]
    [InlineData("sulfur_mines", "mob_fire_lizard", "item_obsidian_key")]
    [InlineData("obsidian_peaks", "mob_fire_raptor", "item_dragon_blood_key")]
    public void KeyItems_Should_Only_Drop_From_Configured_Enemies(
        string location, string enemyId, string expectedItemId)
    {
        Assert.Equal(expectedItemId, GameBackend.Core.Services.BattleService.GetKeyItemReward(location, enemyId));
    }

    [Theory]
    [InlineData("ancient_cave", "boss_goblin_king")]
    [InlineData("forgotten_temple", "boss_shadow_demon")]
    [InlineData("sunken_shipwreck", "boss_shadow_demon")]
    [InlineData("abyssal_trench", "mob_abyssal_spirit")]
    [InlineData("coral_palace", "mob_shadow_spirit")]
    [InlineData("sulfur_mines", "mob_fire_raptor")]
    [InlineData("obsidian_peaks", "mob_adult_dragon")]
    [InlineData("dragon_nest", "boss_dragon_king")]
    public void KeyItems_Should_Not_Drop_From_Wrong_Enemy(string location, string enemyId)
    {
        Assert.Null(GameBackend.Core.Services.BattleService.GetKeyItemReward(location, enemyId));
    }

    [Fact]
    public void Shadow_Demon_Retry_Should_Unlock_After_Two_Mob_Wins()
    {
        var start = DateTime.UtcNow;
        var actions = new List<StoryAction>
        {
            BattleResult(start, "Kết quả: Thất bại. Đối thủ: Shadow Demon (Cấp độ 20)."),
            BattleResult(start.AddMinutes(1), "Kết quả: Chiến thắng. Đối thủ: Shadow Spirit (Cấp độ 5)."),
            BattleResult(start.AddMinutes(2), "Kết quả: Chiến thắng. Đối thủ: Abyssal Spirit (Cấp độ 6).")
        };

        Assert.Equal(2, GameBackend.Core.Services.StoryService.CountBossRetryMobWins(actions));
        Assert.True(GameBackend.Core.Services.StoryService.IsBossRetryUnlocked(actions));
    }

    [Fact]
    public async Task ValidateAndSanitizeAsync_Should_Block_Already_Defeated_Chapter_Boss_And_Remove_Retry_Choice()
    {
        var defeated = new DefeatedBoss
        {
            characterId = "char-1",
            bossId = "boss_shadow_demon",
            bossName = "Shadow Demon",
            defeatedAt = DateTime.UtcNow
        };
        var validator = BuildValidator(
            new FakeContentService(Array.Empty<string>(), Array.Empty<string>(), new[] { "coral_palace" }, Array.Empty<string>()),
            defeatedBosses: new[] { defeated });
        var response = new StoryAiResponse
        {
            TriggerBattle = true,
            BossId = "boss_shadow_demon",
            BossName = "Shadow Demon",
            CurrentLocation = "coral_palace",
            CurrentNodeId = "boss_room",
            NarrativeText = "Bạn tái chiến Shadow Demon.",
            Choices = new List<StoryChoiceOption>
            {
                new() { label = "Tái chiến Shadow Demon", nextNodeId = "boss_room" }
            }
        };

        var sanitized = await validator.ValidateAndSanitizeAsync(
            CreateSession("coral_palace"), CreateCharacter("coral_palace"), response);

        Assert.False(sanitized.TriggerBattle);
        Assert.Null(sanitized.BossId);
        Assert.DoesNotContain(sanitized.Choices, c => c.label.Contains("Shadow Demon", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(sanitized.Choices, c => c.nextNodeId == "continue_after_boss");
    }

    [Fact]
    public async Task ValidateAndSanitizeAsync_Should_Not_Block_Normal_Mob_When_Chapter_Boss_Was_Defeated()
    {
        var defeated = new DefeatedBoss
        {
            characterId = "char-1",
            bossId = "boss_shadow_demon",
            bossName = "Shadow Demon",
            defeatedAt = DateTime.UtcNow
        };
        var validator = BuildValidator(
            new FakeContentService(Array.Empty<string>(), Array.Empty<string>(), new[] { "coral_palace" }, Array.Empty<string>()),
            defeatedBosses: new[] { defeated });
        var response = new StoryAiResponse
        {
            TriggerBattle = true,
            BossId = "mob_shadow_spirit",
            BossName = "Shadow Spirit",
            CurrentLocation = "coral_palace",
            NarrativeText = "Bạn tấn công Shadow Spirit."
        };

        var sanitized = await validator.ValidateAndSanitizeAsync(
            CreateSession("coral_palace"), CreateCharacter("coral_palace"), response);

        Assert.True(sanitized.TriggerBattle);
        Assert.Equal("mob_shadow_spirit", sanitized.BossId);
    }

    [Fact]
    public void BattleService_Should_Require_Exact_Catalog_BossId()
    {
        Assert.Null(GameBackend.Core.Services.BattleService.FindBossTemplate(null));
        Assert.Null(GameBackend.Core.Services.BattleService.FindBossTemplate(""));
        Assert.Null(GameBackend.Core.Services.BattleService.FindBossTemplate("shadow_demon"));
        Assert.Null(GameBackend.Core.Services.BattleService.FindBossTemplate("Shadow Demon"));
        Assert.Equal("boss_shadow_demon",
            GameBackend.Core.Services.BattleService.FindBossTemplate("boss_shadow_demon")?.bossId);
        Assert.Equal("mob_drowned_sailor",
            GameBackend.Core.Services.BattleService.FindBossTemplate("mob_drowned_sailor")?.bossId);
    }

    [Fact]
    public async Task LocationValidator_Should_Block_Chapter3_Location1_To_Location3_And_Align_Node()
    {
        var inventory = new[]
        {
            new Inventory { characterId = "char-1", itemId = "item_obsidian_key", quantity = 1 },
            new Inventory { characterId = "char-1", itemId = "item_dragon_blood_key", quantity = 1 }
        };
        var validator = BuildValidator(
            new FakeContentService(Array.Empty<string>(), Array.Empty<string>(),
                new[] { "sulfur_mines", "obsidian_peaks", "dragon_nest" }, Array.Empty<string>()),
            inventory);
        var session = CreateSession("sulfur_mines");
        session.currentChapterId = "chapter_3";
        session.currentNodeId = "sulfur_mines";
        var response = new StoryAiResponse
        {
            CurrentLocation = "dragon_nest",
            CurrentNodeId = "dragon_nest",
            Choices = new List<StoryChoiceOption>()
        };

        var sanitized = await validator.ValidateAndSanitizeAsync(session, CreateCharacter("sulfur_mines"), response);

        Assert.Equal("sulfur_mines", sanitized.CurrentLocation);
        Assert.Equal("sulfur_mines", sanitized.CurrentNodeId);
        Assert.Contains("Mỏ Lưu Huỳnh", sanitized.NarrativeText);
        Assert.DoesNotContain("Tổ Rồng", sanitized.NarrativeText);
    }

    [Fact]
    public void CoralPalace_Should_Always_Offer_Normal_Mob_Choice()
    {
        var response = new StoryAiResponse
        {
            CurrentLocation = "coral_palace",
            Choices = new List<StoryChoiceOption>
            {
                new() { label = "Đối mặt Shadow Demon", nextNodeId = "boss_room" },
                new() { label = "Rút lui", nextNodeId = "abyssal_trench" },
                new() { label = "Khám phá", nextNodeId = "explore_palace" }
            }
        };

        GameBackend.Core.Services.StoryService.EnsureLocationCombatChoice(response, "coral_palace");

        Assert.Equal(3, response.Choices.Count);
        Assert.Contains(response.Choices, c => c.nextNodeId == "fight_shadow_spirit");
        Assert.Contains(response.Choices, c => c.nextNodeId == "boss_room");
    }

    [Theory]
    [InlineData("goblin_hideout", "đánh lính gác goblin", "mob_goblin_guard")]
    [InlineData("goblin_hideout", "chiến đấu với trinh sát goblin", "mob_goblin_scout")]
    [InlineData("sunken_shipwreck", "tấn công thủy thủ chết đuối", "mob_drowned_sailor")]
    [InlineData("abyssal_trench", "đánh oan hồn biển sâu", "mob_abyssal_spirit")]
    [InlineData("coral_palace", "đánh oan hồn bóng tối", "mob_shadow_spirit")]
    [InlineData("sulfur_mines", "tấn công khủng long lửa", "mob_fire_lizard")]
    [InlineData("obsidian_peaks", "đánh khủng long săn lửa", "mob_fire_raptor")]
    [InlineData("dragon_nest", "chiến đấu với rồng trưởng thành", "mob_adult_dragon")]
    [InlineData("goblin_hideout", "đối mặt Goblin King", "boss_goblin_king")]
    [InlineData("coral_palace", "tái chiến Shadow Demon", "boss_shadow_demon")]
    [InlineData("dragon_nest", "đối mặt Vua Rồng", "boss_dragon_king")]
    public void ResolveCombatTargetId_Should_Map_Exact_Target_For_All_Chapters(
        string location, string input, string expectedId)
    {
        Assert.Equal(expectedId, GameBackend.Core.Services.StoryService.ResolveCombatTargetId(input, location));
    }

    [Theory]
    [InlineData("goblin_hideout", "chapter_1", "mob_goblin_guard")]
    [InlineData("coral_palace", "chapter_2", "mob_shadow_spirit")]
    [InlineData("dragon_nest", "chapter_3", "mob_adult_dragon")]
    public async Task BossRoom_Missing_Target_Should_Fall_Back_To_Mob_Not_Chapter_Boss(
        string location, string chapterId, string expectedMobId)
    {
        var validator = BuildValidator(new FakeContentService(
            Array.Empty<string>(), Array.Empty<string>(), new[] { location }, Array.Empty<string>()));
        var session = CreateSession(location);
        session.currentChapterId = chapterId;
        session.currentNodeId = "boss_room";
        var response = new StoryAiResponse
        {
            TriggerBattle = true,
            BossId = null,
            CurrentLocation = location,
            CurrentNodeId = "boss_room",
            NarrativeText = "Vị vua của khu vực gầm lên trong bóng tối, nhưng bạn tấn công lính canh trước mặt."
        };

        var sanitized = await validator.ValidateAndSanitizeAsync(session, CreateCharacter(location), response);

        Assert.True(sanitized.TriggerBattle);
        Assert.Equal(expectedMobId, sanitized.BossId);
        Assert.StartsWith("mob_", sanitized.BossId, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("forgotten_temple", "đánh Shadow Demon")]
    [InlineData("goblin_hideout", "đánh Vua Rồng")]
    [InlineData("sulfur_mines", "đánh Thủy Thủ Chết Đuối")]
    public void ResolveCombatTargetId_Should_Reject_Target_From_Wrong_Location(string location, string input)
    {
        Assert.Null(GameBackend.Core.Services.StoryService.ResolveCombatTargetId(input, location));
    }

    private static StoryAction BattleResult(DateTime createdAt, string text) => new()
    {
        actionId = Guid.NewGuid().ToString("N"),
        sessionId = "session-1",
        actionType = "battle_result",
        aiResponse = $"[TRẬN ĐÁNH VỪA KẾT THÚC]\n{text}",
        createdAt = createdAt
    };

    private static GameRuleValidator BuildValidator(
        IContentService contentService,
        IEnumerable<Inventory>? inventory = null,
        IEnumerable<DefeatedBoss>? defeatedBosses = null)
    {
        var defeatedBossRepository = new FakeDefeatedBossRepository(defeatedBosses);
        var validators = new IGameRuleSubValidator[]
        {
            new BossValidator(contentService, defeatedBossRepository, NullLogger<BossValidator>.Instance),
            new InventoryValidator(contentService, NullLogger<InventoryValidator>.Instance),
            new LocationValidator(contentService, new FakeInventoryRepository(inventory), NullLogger<LocationValidator>.Instance),
            new CharacterValidator(),
            new StoryValidator()
        };

        return new GameRuleValidator(validators, NullLogger<GameRuleValidator>.Instance);
    }

    private sealed class FakeDefeatedBossRepository : IDefeatedBossRepository
    {
        private readonly List<DefeatedBoss> _bosses;

        public FakeDefeatedBossRepository(IEnumerable<DefeatedBoss>? bosses = null)
        {
            _bosses = bosses?.ToList() ?? new List<DefeatedBoss>();
        }

        public Task SaveDefeatedBossAsync(DefeatedBoss defeatedBoss)
        {
            _bosses.Add(defeatedBoss);
            return Task.CompletedTask;
        }

        public Task<List<DefeatedBoss>> GetDefeatedBossesByCharacterIdAsync(string characterId) =>
            Task.FromResult(_bosses.Where(b => b.characterId == characterId).ToList());

        public Task<bool> HasDefeatedBossAsync(string characterId, string bossId) =>
            Task.FromResult(_bosses.Any(b => b.characterId == characterId &&
                                             string.Equals(b.bossId, bossId, StringComparison.OrdinalIgnoreCase)));

        public Task DeleteByCharacterIdAsync(string characterId)
        {
            _bosses.RemoveAll(b => b.characterId == characterId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        private readonly List<Inventory> _items;

        public FakeInventoryRepository(IEnumerable<Inventory>? items = null)
        {
            _items = items?.ToList() ?? new List<Inventory>();
        }

        public Task<List<Inventory>> GetByCharacterIdAsync(string characterId) => Task.FromResult(_items.ToList());
        public Task<Inventory?> GetByInventoryIdAsync(string inventoryId) => Task.FromResult<Inventory?>(null);
        public Task<Inventory?> FindByCharacterAndItemAsync(string characterId, string itemId) => Task.FromResult<Inventory?>(null);
        public Task<List<Inventory>> GetEquippedItemsAsync(string characterId) => Task.FromResult(new List<Inventory>());
        public Task<int> CountSlotsAsync(string characterId) => Task.FromResult(0);
        public Task SaveAsync(Inventory inventory) => Task.CompletedTask;
        public Task DeleteAsync(string inventoryId) => Task.CompletedTask;
    }

    private static StorySession CreateSession(string location)
    {
        return new StorySession
        {
            sessionId = "session-1",
            characterId = "char-1",
            currentLocation = location,
            currentNodeId = "intro",
            currentChapterId = "chapter_1",
            storySummary = "summary",
            status = "Active",
            sourceType = "AI",
            storyVersion = "1.0",
            updatedAt = DateTime.UtcNow
        };
    }

    private static Character CreateCharacter(string location)
    {
        return new Character
        {
            characterId = "char-1",
            userId = "user-1",
            name = "Hero",
            level = 1,
            experience = 0,
            hp = 90,
            maxHp = 100,
            mp = 20,
            maxMp = 30,
            attack = 10,
            defense = 5,
            criticalRate = 0.1f,
            luckyRate = 0.1f,
            gold = 10,
            className = "Warrior",
            status = "Alive",
            currentLocationId = location,
            reviveTime = DateTime.UtcNow
        };
    }

    private sealed class FakeContentService : IContentService
    {
        private readonly HashSet<string> _validBosses;
        private readonly HashSet<string> _validItems;
        private readonly HashSet<string> _validLocations;
        private readonly HashSet<string> _validQuests;

        public FakeContentService(
            IEnumerable<string> validBosses,
            IEnumerable<string> validItems,
            IEnumerable<string> validLocations,
            IEnumerable<string> validQuests)
        {
            _validBosses = new HashSet<string>(validBosses, StringComparer.OrdinalIgnoreCase);
            _validItems = new HashSet<string>(validItems, StringComparer.OrdinalIgnoreCase);
            _validLocations = new HashSet<string>(validLocations, StringComparer.OrdinalIgnoreCase);
            _validQuests = new HashSet<string>(validQuests, StringComparer.OrdinalIgnoreCase);
        }

        public Task<string> GetWorldAsync() => Task.FromResult("world");
        public Task<string> GetChapterAsync(string chapterId) => Task.FromResult(chapterId);
        public Task<string> GetLocationAsync(string locationId) => Task.FromResult(locationId);
        public Task<string> GetBossAsync(string bossId) => Task.FromResult(bossId);
        public Task<string> GetItemAsync(string itemId) => Task.FromResult(itemId);
        public Task<string> GetQuestAsync(string questId) => Task.FromResult(questId);
        public Task<bool> BossExistsAsync(string bossId) => Task.FromResult(_validBosses.Contains(bossId));
        public Task<bool> ItemExistsAsync(string itemId) => Task.FromResult(_validItems.Contains(itemId));
        public Task<bool> LocationExistsAsync(string locationId) => Task.FromResult(_validLocations.Contains(locationId));
        public Task<bool> QuestExistsAsync(string questId) => Task.FromResult(_validQuests.Contains(questId));
    }
}
