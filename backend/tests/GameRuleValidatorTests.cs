using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        var validator = BuildValidator(
            new FakeBossRepository(exists: false),
            new FakeItemRepository(validItemIds: new[] { "ancient_key" }),
            new FakeLocationRepository(validLocations: new[] { "dragon_cave", "ancient_cave_depths" }),
            new FakeLootRepository(validDrops: new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["dragon_cave"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ancient_key" }
            }));

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
        var validator = BuildValidator(
            new FakeBossRepository(exists: true),
            new FakeItemRepository(validItemIds: Array.Empty<string>()),
            new FakeLocationRepository(validLocations: new[] { "ancient_cave_depths" }),
            new FakeLootRepository(validDrops: new Dictionary<string, HashSet<string>>()));

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

    private static GameRuleValidator BuildValidator(
        IBossRepository bossRepository,
        IItemRepository itemRepository,
        ILocationRepository locationRepository,
        ILootRepository lootRepository)
    {
        var validators = new IGameRuleSubValidator[]
        {
            new BossValidator(bossRepository, locationRepository, NullLogger<BossValidator>.Instance),
            new InventoryValidator(itemRepository, lootRepository, NullLogger<InventoryValidator>.Instance),
            new LocationValidator(locationRepository),
            new QuestValidator(),
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

    private sealed class FakeBossRepository : IBossRepository
    {
        private readonly bool _exists;

        public FakeBossRepository(bool exists)
        {
            _exists = exists;
        }

        public Task<Boss?> GetByIdAsync(string bossId)
        {
            if (!_exists)
            {
                return Task.FromResult<Boss?>(null);
            }

            return Task.FromResult<Boss?>(new Boss
            {
                bossId = bossId,
                name = "Boss",
                level = 10,
                baseHp = 100
            });
        }

        public Task SaveAsync(Boss boss) => Task.CompletedTask;
    }

    private sealed class FakeItemRepository : IItemRepository
    {
        private readonly HashSet<string> _validItems;

        public FakeItemRepository(IEnumerable<string> validItemIds)
        {
            _validItems = new HashSet<string>(validItemIds, StringComparer.OrdinalIgnoreCase);
        }

        public Task<bool> ExistsAsync(string itemId)
        {
            return Task.FromResult(_validItems.Contains(itemId));
        }
    }

    private sealed class FakeLocationRepository : ILocationRepository
    {
        private readonly HashSet<string> _validLocations;

        public FakeLocationRepository(IEnumerable<string> validLocations)
        {
            _validLocations = new HashSet<string>(validLocations, StringComparer.OrdinalIgnoreCase);
        }

        public Task<bool> ExistsAsync(string locationId)
        {
            return Task.FromResult(_validLocations.Contains(locationId));
        }

        public Task<bool> CanSpawnBossAsync(string bossId, string locationId)
        {
            return Task.FromResult(_validLocations.Contains(locationId) && !string.Equals(bossId, "dragon_001", StringComparison.OrdinalIgnoreCase));
        }
    }

    private sealed class FakeLootRepository : ILootRepository
    {
        private readonly Dictionary<string, HashSet<string>> _validDrops;

        public FakeLootRepository(Dictionary<string, HashSet<string>> validDrops)
        {
            _validDrops = validDrops;
        }

        public Task<bool> CanDropItemAtLocationAsync(string itemId, string locationId)
        {
            if (!_validDrops.TryGetValue(locationId, out var allowed))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(allowed.Contains(itemId));
        }
    }
}
