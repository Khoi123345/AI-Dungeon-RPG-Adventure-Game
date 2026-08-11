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

    private static StoryAction BattleResult(DateTime createdAt, string text) => new()
    {
        actionId = Guid.NewGuid().ToString("N"),
        sessionId = "session-1",
        actionType = "battle_result",
        aiResponse = $"[TRẬN ĐÁNH VỪA KẾT THÚC]\n{text}",
        createdAt = createdAt
    };

    private static GameRuleValidator BuildValidator(IContentService contentService, IEnumerable<Inventory>? inventory = null)
    {
        var validators = new IGameRuleSubValidator[]
        {
            new BossValidator(contentService, NullLogger<BossValidator>.Instance),
            new InventoryValidator(contentService, NullLogger<InventoryValidator>.Instance),
            new LocationValidator(contentService, new FakeInventoryRepository(inventory), NullLogger<LocationValidator>.Instance),
            new CharacterValidator(),
            new StoryValidator()
        };

        return new GameRuleValidator(validators, NullLogger<GameRuleValidator>.Instance);
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
