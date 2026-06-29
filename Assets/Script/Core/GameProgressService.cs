using System;
using System.Collections.Generic;
using UnityEngine;
using GameShared.Models;

public class GameProgressService : MonoBehaviour
{
    private static GameProgressService instance;

    public static GameProgressService Instance
    {
        get
        {
            if (instance == null)
            {
                EnsureInstance();
            }

            return instance;
        }
    }

    public User CurrentUser { get; private set; }
    public Character CurrentCharacter { get; private set; }
    public StorySession CurrentStorySession { get; private set; }
    public Boss CurrentBoss { get; private set; }

    private readonly List<Item> items = new List<Item>();
    private readonly List<Inventory> inventory = new List<Inventory>();
    private readonly List<StoryAction> storyActions = new List<StoryAction>();
    private readonly List<LootDrop> lootDrops = new List<LootDrop>();
    private readonly List<Battle> battles = new List<Battle>();
    private readonly List<BossEncounter> encounters = new List<BossEncounter>();

    private bool initialized;

    public static GameProgressService EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject serviceObject = new GameObject(nameof(GameProgressService));
        instance = serviceObject.AddComponent<GameProgressService>();
        DontDestroyOnLoad(serviceObject);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeIfNeeded();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public void InitializeIfNeeded()
    {
        if (initialized)
        {
            return;
        }

        SeedMockWorld();
        initialized = true;
    }

    /// <summary>
    /// Cập nhật CurrentUser sau khi đăng nhập thành công (từ AuthManager).
    /// Giữ nguyên Character, Boss, StorySession nếu đã có.
    /// </summary>
    public void SetCurrentUser(GameShared.Models.User user)
    {
        if (user == null) return;
        CurrentUser = user;
        Debug.Log($"[GameProgressService] CurrentUser set: {user.displayName} (id={user.userId})");
    }

    /// <summary>Xóa session khi logout.</summary>
    public void ClearUser()
    {
        CurrentUser = null;
        CurrentCharacter = null;
        CurrentStorySession = null;
        CurrentBoss = null;
        initialized = false;
        Debug.Log("[GameProgressService] Session cleared.");
    }

    public StoryData CreateStoryDemoData()
    {
        InitializeIfNeeded();

        StoryCharacterState characterState = new StoryCharacterState
        {
            characterName = CurrentCharacter.name,
            level = CurrentCharacter.level,
            hp = CurrentCharacter.hp,
            gold = CurrentCharacter.gold
        };

        string context = CurrentStorySession != null ? CurrentStorySession.storySummary : "";
        string location = CurrentStorySession != null ? CurrentStorySession.currentLocation : "Unknown";

        StoryData storyData = new StoryData
        {
            title = "Dungeon Story",
            node = new StoryNodeData
            {
                nodeId = CurrentStorySession != null ? CurrentStorySession.currentNodeId : "intro",
                backgroundKey = string.Empty,
                character = characterState,
                lines = new List<StoryLineData>
                {
                    new StoryLineData
                    {
                        text = $"{characterState.characterName} đang ở {location}. {context}",
                        pauseAfter = 0.2f
                    },
                    new StoryLineData
                    {
                        text = "Ba con đường mở ra trước mắt: chiến đấu, điều tra hoặc rời đi để hồi sức.",
                        pauseAfter = 0.2f
                    },
                    new StoryLineData
                    {
                        text = "Mỗi lựa chọn sẽ cập nhật session, character và battle state như trong schema của bạn.",
                        pauseAfter = 0.2f
                    }
                },
                choices = new List<StoryChoiceData>
                {
                    new StoryChoiceData { label = "Tấn công", description = "Đi sang nhánh chiến đấu", nextNodeId = "battle_path" },
                    new StoryChoiceData { label = "Điều tra", description = "Tăng gold và exp nếu thành công", nextNodeId = "investigate_path" },
                    new StoryChoiceData { label = "Nghỉ ngơi", description = "Hồi phục HP và MP", nextNodeId = "rest_path" }
                }
            }
        };

        return storyData;
    }

    public StoryAction RecordStoryAction(int choiceIndex, string playerInput, string aiResponse)
    {
        InitializeIfNeeded();

        StoryChoice choice = GetChoice(choiceIndex);
        ApplyChoiceEffect(choice);

        StoryAction action = new StoryAction
        {
            actionId = Guid.NewGuid().ToString("N"),
            sessionId = CurrentStorySession.sessionId,
            playerInput = playerInput,
            aiResponse = aiResponse,
            choiceIndex = choiceIndex,
            actionType = "choice",
            metadataJson = "{}",
            createdAt = DateTime.UtcNow
        };

        storyActions.Add(action);
        CurrentStorySession.updatedAt = DateTime.UtcNow;
        CurrentStorySession.currentNodeId = choice != null ? choice.nextNodeId : CurrentStorySession.currentNodeId;

        return action;
    }

    public BattleData CreateBattleDemoData()
    {
        InitializeIfNeeded();

        FighterStats player = new FighterStats
        {
            name = CurrentCharacter.name,
            level = CurrentCharacter.level,
            maxHP = CurrentCharacter.maxHp,
            currentHP = CurrentCharacter.hp
        };

        FighterStats boss = new FighterStats
        {
            name = CurrentBoss.name,
            level = 15,
            maxHP = CurrentBoss.baseHp,
            currentHP = CurrentBoss.baseHp
        };

        // Tăng sát thương người chơi lên (nhân 3) để đảm bảo boss bị tiêu diệt và người chơi thắng trận khi test
        int playerDamage = Mathf.Max(1, (CurrentCharacter.attack + CurrentCharacter.level * 2 - CurrentBoss.baseDefense) * 3);
        int bossDamage = Mathf.Max(1, CurrentBoss.baseAttack - CurrentCharacter.defense / 2);

        List<BattleTurn> turns = new List<BattleTurn>();
        int playerHp = player.currentHP;
        int bossHp = boss.currentHP;
        bool isVictory = false;

        for (int turn = 0; turn < 4 && playerHp > 0 && bossHp > 0; turn++)
        {
            bossHp = Mathf.Max(0, bossHp - playerDamage);
            turns.Add(new BattleTurn
            {
                logMessage = $"{player.name} gây {playerDamage} sát thương lên {boss.name}.",
                playerHPRemaining = playerHp,
                bossHPRemaining = bossHp,
                isCritical = turn == 0
            });

            if (bossHp <= 0)
            {
                isVictory = true;
                break;
            }

            playerHp = Mathf.Max(0, playerHp - bossDamage);
            turns.Add(new BattleTurn
            {
                logMessage = $"{boss.name} phản công gây {bossDamage} sát thương.",
                playerHPRemaining = playerHp,
                bossHPRemaining = bossHp,
                isCritical = false
            });
        }

        BattleData battleData = new BattleData
        {
            player = player,
            boss = boss,
            turns = turns,
            isPlayerVictory = isVictory
        };

        return battleData;
    }

    public List<LootDrop> RecordBattleResult(BattleData battleData, bool isVictory)
    {
        InitializeIfNeeded();
        List<LootDrop> dropped = new List<LootDrop>();

        BossEncounter encounter = new BossEncounter
        {
            encounterId = Guid.NewGuid().ToString("N"),
            characterId = CurrentCharacter.characterId,
            bossId = CurrentBoss.bossId,
            bossLevel = CurrentBoss.level,
            playerHpBefore = CurrentCharacter.hp,
            playerHpAfter = battleData.turns.Count > 0 ? battleData.turns[battleData.turns.Count - 1].playerHPRemaining : CurrentCharacter.hp,
            bossHpBefore = CurrentBoss.baseHp,
            bossHpAfter = battleData.turns.Count > 0 ? battleData.turns[battleData.turns.Count - 1].bossHPRemaining : CurrentBoss.baseHp,
            status = isVictory ? "Victory" : "Defeat",
            encounterTime = DateTime.UtcNow
        };

        encounters.Add(encounter);

        Battle battle = new Battle
        {
            battleId = Guid.NewGuid().ToString("N"),
            encounterId = encounter.encounterId,
            playerPower = CurrentCharacter.attack + CurrentCharacter.level * 2,
            bossPower = CurrentBoss.baseAttack,
            battleType = "Boss",
            status = isVictory ? "Completed" : "Failed",
            result = isVictory ? "Victory" : "Defeat",
            turnCount = battleData.turns != null ? battleData.turns.Count : 0,
            durationMs = Mathf.Max(1, (battleData.turns != null ? battleData.turns.Count : 0) * 1200),
            playerSnapshotJson = JsonUtility.ToJson(CurrentCharacter),
            bossSnapshotJson = JsonUtility.ToJson(CurrentBoss),
            rewardJson = isVictory ? "{\"gold\":100,\"exp\":75}" : "{}",
            battleTime = DateTime.UtcNow
        };

        battles.Add(battle);

        if (isVictory)
        {
            CurrentCharacter.gold += CurrentBoss.goldReward;
            CurrentCharacter.experience += CurrentBoss.expReward;
            HandleLevelUpIfNeeded();

            LootDrop loot = new LootDrop
            {
                lootId = Guid.NewGuid().ToString("N"),
                battleId = battle.battleId,
                itemId = items.Count > 0 ? items[0].itemId : string.Empty,
                quantity = 1,
                dropRate = 1f,
                sourceType = "BossDrop",
                isUnique = false,
                createdAt = DateTime.UtcNow
            };

            lootDrops.Add(loot);
            dropped.Add(loot);

            if (!string.IsNullOrEmpty(loot.itemId))
            {
                AddItemToInventory(loot.itemId, loot.quantity, false);
            }
        }

        int resolvedPlayerHp = battleData.turns != null && battleData.turns.Count > 0
            ? battleData.turns[battleData.turns.Count - 1].playerHPRemaining
            : battleData.player.currentHP;

        CurrentCharacter.hp = Mathf.Clamp(resolvedPlayerHp, 0, CurrentCharacter.maxHp);
        return dropped;
    }

    public IReadOnlyList<Inventory> GetInventory()
    {
        return inventory;
    }

    private void SeedMockWorld()
    {
        CurrentUser = new User
        {
            userId = Guid.NewGuid().ToString("N"),
            username = "player01",
            email = "player01@example.com",
            passwordHash = "mock-hash",
            displayName = "Dungeon Rider",
            status = "Active",
            createdAt = DateTime.UtcNow.AddDays(-3),
            lastLoginAt = DateTime.UtcNow
        };

        CurrentCharacter = new Character
        {
            characterId = Guid.NewGuid().ToString("N"),
            userId = CurrentUser.userId,
            name = "Player_Name",
            level = 7,
            experience = 240,
            hp = 84,
            maxHp = 120,
            mp = 30,
            maxMp = 40,
            attack = 18,
            defense = 8,
            criticalRate = 0.12f,
            luckyRate = 0.08f,
            gold = 120,
            className = "Adventurer",
            status = "Alive",
            currentLocationId = "ruins_gate",
            reviveTime = DateTime.UtcNow
        };

        items.Clear();
        items.Add(new Item
        {
            itemId = Guid.NewGuid().ToString("N"),
            name = "Rusty Sword",
            rarity = "Common",
            itemType = "Weapon",
            attackBonus = 4,
            defenseBonus = 0,
            hpBonus = 0,
            criticalBonus = 0.01f,
            imageUrl = string.Empty,
            description = "Thanh kiếm cũ nhưng vẫn còn hữu dụng.",
            stackable = false,
            sellPrice = 15,
            buyPrice = 35,
            requiredLevel = 1,
            slotType = "Weapon",
            effectJson = "{}"
        });

        inventory.Clear();
        inventory.Add(new Inventory
        {
            inventoryId = Guid.NewGuid().ToString("N"),
            characterId = CurrentCharacter.characterId,
            itemId = items[0].itemId,
            quantity = 1,
            equipped = true,
            slotIndex = 0,
            locked = false,
            acquiredAt = DateTime.UtcNow.AddDays(-1)
        });

        CurrentBoss = new Boss
        {
            bossId = Guid.NewGuid().ToString("N"),
            name = "Shadow Demon",
            rarity = "Rare",
            level = 15,
            baseHp = 200,
            baseAttack = 22,
            baseDefense = 9,
            speed = 12,
            criticalRate = 0.15f,
            imageUrl = string.Empty,
            expReward = 75,
            goldReward = 100,
            skillSetJson = "[]"
        };

        CurrentStorySession = new StorySession
        {
            sessionId = Guid.NewGuid().ToString("N"),
            characterId = CurrentCharacter.characterId,
            currentLocation = "Ancient Ruins",
            currentNodeId = "intro",
            status = "Active",
            updatedAt = DateTime.UtcNow,
            endedAt = null,
            storyVersion = "v1",
            sourceType = "mock"
        };

        storyActions.Clear();
        lootDrops.Clear();
        battles.Clear();
    }

    private void ApplyChoiceEffect(StoryChoice choice)
    {
        if (choice == null)
        {
            return;
        }

        CurrentCharacter.gold = Mathf.Max(0, CurrentCharacter.gold + choice.goldDelta);
        CurrentCharacter.hp = Mathf.Clamp(CurrentCharacter.hp + choice.hpDelta, 1, CurrentCharacter.maxHp);
        CurrentCharacter.experience = Mathf.Max(0, CurrentCharacter.experience + choice.expDelta);
        HandleLevelUpIfNeeded();
    }

    private StoryChoice GetChoice(int choiceIndex)
    {
        if (choiceIndex == 0)
        {
            return new StoryChoice
            {
                label = "Tấn công",
                description = "Đi sang nhánh chiến đấu",
                nextNodeId = "battle_path",
                goldDelta = 0,
                hpDelta = 0,
                expDelta = 25
            };
        }

        if (choiceIndex == 1)
        {
            return new StoryChoice
            {
                label = "Điều tra",
                description = "Tăng gold và exp nếu thành công",
                nextNodeId = "investigate_path",
                goldDelta = 15,
                hpDelta = 0,
                expDelta = 15
            };
        }

        return new StoryChoice
        {
            label = "Nghỉ ngơi",
            description = "Hồi phục HP và MP",
            nextNodeId = "rest_path",
            goldDelta = -5,
            hpDelta = 18,
            expDelta = 5
        };
    }

    private void HandleLevelUpIfNeeded()
    {
        int requiredExperience = CurrentCharacter.level * 100;
        while (CurrentCharacter.experience >= requiredExperience)
        {
            CurrentCharacter.experience -= requiredExperience;
            CurrentCharacter.level += 1;
            CurrentCharacter.maxHp += 12;
            CurrentCharacter.hp = CurrentCharacter.maxHp;
            CurrentCharacter.maxMp += 5;
            CurrentCharacter.mp = CurrentCharacter.maxMp;
            CurrentCharacter.attack += 3;
            CurrentCharacter.defense += 2;
            requiredExperience = CurrentCharacter.level * 100;
        }
    }

    private void AddItemToInventory(string itemId, int quantity, bool equipped)
    {
        Inventory existing = inventory.Find(entry => entry.itemId == itemId && entry.equipped == equipped);
        if (existing != null)
        {
            existing.quantity += quantity;
            return;
        }

        inventory.Add(new Inventory
        {
            inventoryId = Guid.NewGuid().ToString("N"),
            characterId = CurrentCharacter.characterId,
            itemId = itemId,
            quantity = quantity,
            equipped = equipped,
            slotIndex = inventory.Count,
            locked = false,
            acquiredAt = DateTime.UtcNow
        });
    }
}