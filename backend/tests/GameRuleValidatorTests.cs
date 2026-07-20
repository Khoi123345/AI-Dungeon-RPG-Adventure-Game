using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameBackend.Core.AIStory.Services;
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
    public async Task ValidateAndSanitizeAsync_Should_Clamp_Character_Abuse_Deltas()
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

        Assert.Equal(50, sanitized.CharacterDelta.HpDelta);
        Assert.Equal(1000, sanitized.CharacterDelta.GoldDelta);
        Assert.Equal(500, sanitized.CharacterDelta.ExpDelta);
        Assert.Equal(40, sanitized.CharacterDelta.MpDelta);
        Assert.Equal("Alive", sanitized.CharacterDelta.Status);
        Assert.DoesNotContain("Dragon chết", sanitized.NarrativeText, StringComparison.OrdinalIgnoreCase);
    }

    private static GameRuleValidator BuildValidator(IContentService contentService)
    {
        var validators = new IGameRuleSubValidator[]
        {
            new BossValidator(contentService, NullLogger<BossValidator>.Instance),
            new InventoryValidator(contentService, NullLogger<InventoryValidator>.Instance),
            new LocationValidator(contentService, NullLogger<LocationValidator>.Instance),
            new CharacterValidator(),
            new StoryValidator()
        };

        return new GameRuleValidator(validators, NullLogger<GameRuleValidator>.Instance);
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
