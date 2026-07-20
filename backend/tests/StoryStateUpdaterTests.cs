using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services;
using GameBackend.Core.Services.Interfaces;
using GameShared.DTOs.Story;
using GameShared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;


public class StoryStateUpdaterTests
{
    [Fact]
    public async Task ApplyAsync_Should_Update_All_Supported_States()
    {
        var session = new StorySession
        {
            sessionId = "session-1",
            characterId = "char-1",
            currentLocation = "ancient_cave",
            currentNodeId = "intro",
            currentChapterId = "chapter_1",
            status = "Active",
            updatedAt = DateTime.UtcNow,
            storyVersion = "1.0",
            storySummary = "Player defeated Goblin King",
            sourceType = "AI"
        };

        var character = new Character
        {
            characterId = "char-1",
            userId = "user-1",
            name = "Hero",
            level = 1,
            experience = 0,
            hp = 80,
            maxHp = 100,
            mp = 20,
            maxMp = 30,
            attack = 10,
            defense = 5,
            criticalRate = 0.05f,
            luckyRate = 0.05f,
            gold = 50,
            className = "Adventurer",
            status = "Alive",
            currentLocationId = "ancient_cave",
            reviveTime = DateTime.UtcNow
        };

        var inventoryRepo = new FakeInventoryRepository(new List<Inventory>
        {
            new Inventory
            {
                inventoryId = "inv-1",
                characterId = "char-1",
                itemId = "rusty_sword",
                quantity = 1,
                equipped = true,
                slotIndex = 0,
                locked = false,
                acquiredAt = DateTime.UtcNow
            }
        });

        var bossRepo = new FakeBossRepository(new Boss
        {
            bossId = "boss_goblin_chief",
            name = "Goblin Chief",
            rarity = "Rare",
            level = 10,
            baseHp = 120,
            baseAttack = 14,
            baseDefense = 8,
            speed = 10,
            criticalRate = 0.1f,
            imageUrl = string.Empty,
            expReward = 75,
            goldReward = 100,
            skillSetJson = "[]"
        });

        var storyRepo = new FakeStoryRepository();
        var characterRepo = new FakeCharacterRepository(character);
        var characterService = new CharacterService(characterRepo, NullLogger<CharacterService>.Instance);
        var battleRepo = new FakeBattleRepository();

        var updater = new StoryStateUpdater(
            storyRepo,
            characterService,
            inventoryRepo,
            battleRepo,
            bossRepo,
            NullLogger<StoryStateUpdater>.Instance);

        var aiResponse = new StoryAiResponse
        {
            NarrativeText = "Bạn mở cánh cửa cổ và thấy ánh sáng bạc tràn ra.",
            CurrentNodeId = "ancient_gate_open",
            CurrentLocation = "ancient_cave_depths",
            CurrentChapterId = "chapter_2",
            StorySummary = "Player opened the ancient door",
            ActionType = "player_action",
            MetadataJson = "{}",
            TriggerBattle = true,
            BossId = "boss_goblin_chief",
            BossLevel = 10,
            CharacterDelta = new StoryAiCharacterDelta
            {
                HpDelta = -12,
                GoldDelta = 5,
                ExpDelta = 25,
                MpDelta = -3,
                Status = "Alive",
                CurrentLocationId = "ancient_cave_depths"
            },
            InventoryChanges = new List<StoryAiInventoryChange>
            {
                new StoryAiInventoryChange
                {
                    ItemId = "ancient_key",
                    ItemName = "Ancient Key",
                    QuantityDelta = 1,
                    Equipped = false,
                    SlotIndex = 2,
                    Locked = false
                }
            }
        };

        await updater.ApplyAsync(session, character, aiResponse);

        Assert.Equal("ancient_gate_open", session.currentNodeId);
        Assert.Equal("ancient_cave_depths", session.currentLocation);
        Assert.Equal("chapter_2", session.currentChapterId);
        Assert.Contains("Player opened the ancient door", session.storySummary);

        Assert.Equal(68, character.hp);
        Assert.Equal(55, character.gold);
        Assert.Equal(25, character.experience);
        Assert.Equal("ancient_cave_depths", character.currentLocationId);

        Assert.NotNull(storyRepo.SavedSession);
        Assert.Equal(session.currentNodeId, storyRepo.SavedSession!.currentNodeId);

        var updatedInventory = await inventoryRepo.GetByCharacterIdAsync("char-1");
        Assert.Equal(2, updatedInventory.Count);
        Assert.Contains(updatedInventory, x => x.itemId == "ancient_key" && x.quantity == 1 && x.equipped == false);

        Assert.NotNull(battleRepo.SavedEncounter);
        Assert.Equal("boss_goblin_chief", battleRepo.SavedEncounter!.bossId);
        Assert.Equal(10, battleRepo.SavedEncounter.bossLevel);
    }

    private sealed class FakeStoryRepository : IStoryRepository
    {
        public StorySession? SavedSession { get; private set; }

        public Task<StorySession?> GetSessionByIdAsync(string sessionId) => Task.FromResult<StorySession?>(SavedSession);
        public Task<StorySession?> GetSessionByCharacterIdAsync(string characterId) => Task.FromResult<StorySession?>(SavedSession);
        public Task SaveSessionAsync(StorySession session)
        {
            SavedSession = session;
            return Task.CompletedTask;
        }
        public Task SaveActionAsync(StoryAction action) => Task.CompletedTask;
        public Task<List<StoryAction>> GetActionsBySessionIdAsync(string sessionId) => Task.FromResult(new List<StoryAction>());
    }

    private sealed class FakeCharacterRepository : ICharacterRepository
    {
        private Character _character;

        public FakeCharacterRepository(Character character)
        {
            _character = character;
        }

        public Task<Character?> GetByIdAsync(string characterId) => Task.FromResult<Character?>(_character);
        public Task<List<Character>> GetByUserIdAsync(string userId) => Task.FromResult(new List<Character>());
        public Task SaveAsync(Character character)
        {
            _character = character;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        private readonly List<Inventory> _items;

        public FakeInventoryRepository(List<Inventory> items)
        {
            _items = items;
        }

        public Task<List<Inventory>> GetByCharacterIdAsync(string characterId) => Task.FromResult(_items.Where(x => x.characterId == characterId).ToList());

        public Task<Inventory?> FindByCharacterAndItemAsync(string characterId, string itemId)
            => Task.FromResult(_items.FirstOrDefault(x => x.characterId == characterId && x.itemId == itemId));

        public Task SaveAsync(Inventory inventory)
        {
            var existing = _items.FirstOrDefault(x => x.inventoryId == inventory.inventoryId);
            if (existing == null)
            {
                _items.Add(inventory);
            }
            else
            {
                existing.quantity = inventory.quantity;
                existing.equipped = inventory.equipped;
                existing.slotIndex = inventory.slotIndex;
                existing.locked = inventory.locked;
            }

            return Task.CompletedTask;
        }

        public Task<Inventory?> GetByInventoryIdAsync(string inventoryId) => Task.FromResult(_items.FirstOrDefault(x => x.inventoryId == inventoryId));
        public Task<List<Inventory>> GetEquippedItemsAsync(string characterId) => Task.FromResult(_items.Where(x => x.characterId == characterId && x.equipped).ToList());
        public Task<int> CountSlotsAsync(string characterId) => Task.FromResult(_items.Count(x => x.characterId == characterId));
        public Task DeleteAsync(string inventoryId)
        {
            _items.RemoveAll(x => x.inventoryId == inventoryId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeBattleRepository : IBattleRepository
    {
        public BossEncounter? SavedEncounter { get; private set; }

        public Task<BossEncounter?> GetEncounterByIdAsync(string encounterId) => Task.FromResult<BossEncounter?>(null);
        public Task SaveEncounterAsync(BossEncounter encounter)
        {
            SavedEncounter = encounter;
            return Task.CompletedTask;
        }

        public Task<Battle?> GetBattleByIdAsync(string battleId) => Task.FromResult<Battle?>(null);
        public Task SaveBattleAsync(Battle battle) => Task.CompletedTask;
        public Task SaveLootDropAsync(LootDrop lootDrop) => Task.CompletedTask;
    }

    private sealed class FakeBossRepository : IBossRepository
    {
        private readonly Boss _boss;

        public FakeBossRepository(Boss boss)
        {
            _boss = boss;
        }

        public Task<Boss?> GetByIdAsync(string bossId) => Task.FromResult<Boss?>(_boss);
        public Task SaveAsync(Boss boss) => Task.CompletedTask;
    }
}