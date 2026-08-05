using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameShared.Models;
using GameShared.DTOs.Character;

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
    private readonly List<CharacterTitle> titles = new List<CharacterTitle>();

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

    /// <summary>
    /// Cập nhật CurrentCharacter sau khi tạo hoặc tải nhân vật từ API backend.
    /// </summary>
    public void SetCurrentCharacter(Character character)
    {
        if (character == null) return;
        CurrentCharacter = character;
        Debug.Log($"[GameProgressService] CurrentCharacter set: {character.name} (id={character.characterId})");
    }

    /// <summary>
    /// Cập nhật thông tin StorySession từ API response của backend.
    /// </summary>
    public void SetCurrentStorySession(string sessionId, string currentNodeId = "intro", string currentLocation = "Ancient Ruins")
    {
        if (CurrentStorySession == null)
        {
            CurrentStorySession = new StorySession
            {
                sessionId = string.IsNullOrEmpty(sessionId) ? Guid.NewGuid().ToString("N") : sessionId,
                characterId = CurrentCharacter?.characterId ?? Guid.NewGuid().ToString("N"),
                currentLocation = currentLocation,
                currentNodeId = currentNodeId,
                status = "Active",
                updatedAt = DateTime.UtcNow,
                storyVersion = "1.0",
                sourceType = "AI"
            };
        }
        else
        {
            if (!string.IsNullOrEmpty(sessionId)) CurrentStorySession.sessionId = sessionId;
            if (!string.IsNullOrEmpty(currentNodeId)) CurrentStorySession.currentNodeId = currentNodeId;
            if (!string.IsNullOrEmpty(currentLocation)) CurrentStorySession.currentLocation = currentLocation;
            CurrentStorySession.updatedAt = DateTime.UtcNow;
        }

        Debug.Log($"[GameProgressService] StorySession updated: sessionId={CurrentStorySession.sessionId}, node={CurrentStorySession.currentNodeId}");
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

    /// <summary>
    /// Spawn một boss ngẫu nhiên theo đúng bảng xác suất rarity của thiết kế gốc.
    /// Tính boss level theo công thức: PlayerLevel + RarityModifier + Random(-3, +3).
    /// Gán vào CurrentBoss để BattlePresenter dùng khi load BattleScene.
    /// </summary>
    public void SpawnRandomBoss()
    {
        InitializeIfNeeded();

        int playerLevel = CurrentCharacter != null ? CurrentCharacter.level : 1;
        string selectedRarity;
        int rarityModifier;

        // Nếu người chơi ở Level thấp (<= 3), ép 100% xuất hiện Boss Common vừa sức để chơi mượt mà
        if (playerLevel <= 3)
        {
            selectedRarity = "Common";
            rarityModifier = 0;
        }
        else
        {
            selectedRarity = GameShared.Config.GameConstants.RollBossRarity();
            rarityModifier = GameShared.Config.GameConstants.GetBossRarityLevelModifier(selectedRarity);
        }

        // Lấy danh sách boss từ GameShared.Config.GameConstants.BossCatalog (Dùng chung với Backend 100%)
        var candidates = GameShared.Config.GameConstants.BossCatalog
            .Where(b => b != null && b.rarity.Equals(selectedRarity, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (candidates.Count == 0) candidates = GameShared.Config.GameConstants.BossCatalog;

        Boss picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];

        // ── Tính Boss Level: Ở level 1-3 thì Boss Level chính xác = Player Level ─────
        int randomModifier = (playerLevel <= 3) ? 0 : UnityEngine.Random.Range(-1, 2);
        int bossLevel = (playerLevel <= 3) ? playerLevel : Mathf.Max(1, playerLevel + rarityModifier + randomModifier);

        CurrentBoss = new Boss
        {
            bossId       = picked.bossId,  // Dùng đúng bossId của GameConstants
            name         = picked.name,
            rarity       = picked.rarity,
            level        = bossLevel,
            baseHp       = picked.baseHp + bossLevel * 5,
            baseAttack   = picked.baseAttack + bossLevel,
            baseDefense  = picked.baseDefense,
            speed        = picked.speed,
            criticalRate = picked.criticalRate,
            expReward    = picked.expReward,
            goldReward   = picked.goldReward,
            skillSetJson = "[]",
            imageUrl     = string.Empty
        };

        Debug.Log($"[GameProgressService] SpawnRandomBoss: {CurrentBoss.name} (bossId={CurrentBoss.bossId}, Rarity={CurrentBoss.rarity}, Level={CurrentBoss.level}, HP={CurrentBoss.baseHp})");
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

    public StoryData ExecuteCustomStoryAction(string playerInput)
    {
        InitializeIfNeeded();

        StoryCharacterState characterState = new StoryCharacterState
        {
            characterName = CurrentCharacter.name,
            level = CurrentCharacter.level,
            hp = CurrentCharacter.hp,
            gold = CurrentCharacter.gold
        };

        string dynamicStoryResponse = GenerateStoryResponseFromInput(playerInput);

        StoryAction action = new StoryAction
        {
            actionId = Guid.NewGuid().ToString("N"),
            sessionId = CurrentStorySession != null ? CurrentStorySession.sessionId : Guid.NewGuid().ToString("N"),
            playerInput = playerInput,
            aiResponse = dynamicStoryResponse,
            choiceIndex = -1,
            actionType = "custom_input",
            metadataJson = "{}",
            createdAt = DateTime.UtcNow
        };

        storyActions.Add(action);
        if (CurrentStorySession != null)
        {
            CurrentStorySession.updatedAt = DateTime.UtcNow;
        }

        StoryData storyData = new StoryData
        {
            title = "Dungeon Story Continuation",
            node = new StoryNodeData
            {
                nodeId = "custom_node_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                backgroundKey = string.Empty,
                character = characterState,
                lines = new List<StoryLineData>
                {
                    new StoryLineData
                    {
                        text = dynamicStoryResponse,
                        pauseAfter = 0.2f
                    }
                }
            }
        };

        return storyData;
    }

    private string GenerateStoryResponseFromInput(string playerInput)
    {
        if (string.IsNullOrWhiteSpace(playerInput))
        {
            return "Bạn phân vân không biết phải làm gì tiếp theo...";
        }

        string inputLower = playerInput.ToLower();

        if (inputLower.Contains("kiếm") || inputLower.Contains("chém") || inputLower.Contains("tấn công") || inputLower.Contains("đánh"))
        {
            return $"Bạn quyết định hành động: '{playerInput}'. Bạn vung vũ khí xé rách màn đêm! Sức mạnh khí thế khiến bầu không khí xung quanh rung chuyển. Một giọng nói vang vọng từ hầm ngục: 'Dũng khí tốt đấy, kẻ phiêu lưu!'";
        }

        if (inputLower.Contains("xem") || inputLower.Contains("kiểm tra") || inputLower.Contains("nhìn") || inputLower.Contains("soi"))
        {
            return $"Bạn tiến lại gần và quan sát tỉ mỉ: '{playerInput}'. Ánh sáng phản chiếu tiết lộ những ký tự cổ xưa ẩn giấu đằng sau bức tường đá. Bạn thu thập thêm được một số thông tin quan trọng.";
        }

        if (inputLower.Contains("chạy") || inputLower.Contains("rút") || inputLower.Contains("né") || inputLower.Contains("tránh"))
        {
            return $"Bạn nhanh chóng thực hiện: '{playerInput}'. Bạn né lùi lại phía sau an toàn, nhịp thở dồn dập trong bóng tối trong khi chờ đợi biến cố tiếp theo.";
        }

        return $"Bạn thực hiện hành động: '{playerInput}'. Mọi chuyển động của bạn đều làm thay đổi vận mệnh trong hầm ngục cổ xưa này. Bóng tối xung quanh dường như đang phản ứng lại quyết định của bạn!";
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

            // Roll ngẫu nhiên vật phẩm từ GameConstants theo rarity của Boss
            var droppedItemTemplate = GameShared.Config.GameConstants.RollRandomItemByRarity(CurrentBoss?.rarity ?? "Common")
                                   ?? GameShared.Config.GameConstants.GetItemById("item_rusty_sword");

            string droppedItemId = droppedItemTemplate != null ? droppedItemTemplate.itemId : "item_rusty_sword";

            LootDrop loot = new LootDrop
            {
                lootId = Guid.NewGuid().ToString("N"),
                battleId = battle.battleId,
                itemId = droppedItemId,
                quantity = 1,
                dropRate = 1f,
                sourceType = "BossDrop",
                isUnique = false,
                createdAt = DateTime.UtcNow
            };

            lootDrops.Add(loot);
            dropped.Add(loot);

            // GHI CHÚ: Không gọi AddItemToInventory ở đây nữa để tránh bị trùng lặp 2 lần! 
            // Vật phẩm đã chọn sẽ được chính thức thêm vào CSDL khi người chơi nhấn nút Confirm ở màn hình Victory.
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

    /// <summary>Trả về danh sách danh hiệu của nhân vật hiện tại.</summary>
    public IReadOnlyList<CharacterTitle> GetTitles()
    {
        return titles;
    }

    /// <summary>
    /// Trả về tối đa <paramref name="maxCount"/> lịch sử phiêu lưu gần nhất,
    /// được ghép từ BossEncounter + Battle.
    /// </summary>
    public List<AdventureRecord> GetBattleHistory(int maxCount = 10)
    {
        List<AdventureRecord> records = new List<AdventureRecord>();

        // Duyệt ngược từ encounter mới nhất
        for (int i = encounters.Count - 1; i >= 0 && records.Count < maxCount; i--)
        {
            BossEncounter enc = encounters[i];

            // Tìm Boss tương ứng (trong mock chỉ có 1 boss)
            string bossName = CurrentBoss != null && CurrentBoss.bossId == enc.bossId
                ? CurrentBoss.name
                : "Unknown Boss";
            string bossRarity = CurrentBoss != null && CurrentBoss.bossId == enc.bossId
                ? CurrentBoss.rarity
                : "Common";

            // Lấy battle khớp với encounter
            Battle battle = battles.Find(b => b.encounterId == enc.encounterId);
            int expGained = 0;
            int goldGained = 0;
            int turnCount = battle != null ? battle.turnCount : 0;

            if (enc.status == "Victory" && CurrentBoss != null && CurrentBoss.bossId == enc.bossId)
            {
                expGained = CurrentBoss.expReward;
                goldGained = CurrentBoss.goldReward;
            }

            records.Add(new AdventureRecord
            {
                encounterId  = enc.encounterId,
                bossName     = bossName,
                bossRarity   = bossRarity,
                bossLevel    = enc.bossLevel,
                result       = enc.status,
                expGained    = expGained,
                goldGained   = goldGained,
                turnCount    = turnCount,
                encounterTime = enc.encounterTime
            });
        }

        return records;
    }

    /// <summary>
    /// Tổng hợp toàn bộ thông tin nhân vật thành <see cref="ProfileCharacterData"/> DTO
    /// để ProfilePresenter render màn hình Profile.
    /// </summary>
    public ProfileCharacterData BuildProfileData()
    {
        InitializeIfNeeded();
        Character c = CurrentCharacter;

        // ── Slots trang bị ───────────────────────────────────────────
        List<ProfileEquippedSlot> slots = new List<ProfileEquippedSlot>
        {
            BuildSlot("Weapon"),
            BuildSlot("Armor"),
            BuildSlot("Accessory"),
            BuildSlot("Ring"),
            BuildSlot("Helmet"),
            BuildSlot("Boots")
        };

        // ── Titles ───────────────────────────────────────────────────
        List<ProfileTitleEntry> titleEntries = new List<ProfileTitleEntry>();
        foreach (CharacterTitle t in titles)
        {
            titleEntries.Add(new ProfileTitleEntry
            {
                titleId     = t.titleId,
                name        = t.name,
                description = t.description,
                rarity      = t.rarity,
                isEquipped  = t.isEquipped,
                earnedAt    = t.earnedAt
            });
        }

        return new ProfileCharacterData
        {
            characterId          = c.characterId,
            characterName        = c.name,
            className            = c.className,
            level                = c.level,
            experience           = c.experience,
            experienceToNextLevel = c.level * 100,
            hp                   = c.hp,
            maxHp                = c.maxHp,
            mp                   = c.mp,
            maxMp                = c.maxMp,
            gold                 = c.gold,
            status               = c.status,
            currentLocationId    = c.currentLocationId,
            attack               = c.attack,
            defense              = c.defense,
            criticalRate         = c.criticalRate,
            luckyRate            = c.luckyRate,
            speed                = c.speed,
            evasionRate          = c.evasionRate,
            magicResist          = c.magicResist,
            equippedSlots        = slots,
            titles               = titleEntries,
            adventureHistory     = GetBattleHistory(10)
        };
    }

    private ProfileEquippedSlot BuildSlot(string slotType)
    {
        Inventory inv = inventory.Find(i => i.equipped && i.slotIndex >= 0
            && items.Find(it => it.itemId == i.itemId)?.slotType == slotType);

        if (inv == null)
        {
            return new ProfileEquippedSlot { slotType = slotType, isEmpty = true };
        }

        Item item = items.Find(it => it.itemId == inv.itemId);
        if (item == null)
        {
            return new ProfileEquippedSlot { slotType = slotType, isEmpty = true };
        }

        return new ProfileEquippedSlot
        {
            slotType       = slotType,
            isEmpty        = false,
            itemId         = item.itemId,
            itemName       = item.name,
            itemRarity     = item.rarity,
            itemDescription = item.description,
            attackBonus    = item.attackBonus,
            defenseBonus   = item.defenseBonus,
            hpBonus        = item.hpBonus,
            criticalBonus  = item.criticalBonus
        };
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
            attack = 18,
            defense = 8,
            criticalRate = 0.12f,
            luckyRate = 0.08f,
            gold = 120,
            className = "Adventurer",
            status = "Alive",
            currentLocationId = "ruins_gate",
            reviveTime = DateTime.UtcNow,
            // Hidden stats
            speed = 12f,
            evasionRate = 0.07f,
            magicResist = 8f
        };

        items.Clear();
        foreach (var it in GameShared.Config.GameConstants.ItemCatalog)
            items.Add(it);

        inventory.Clear();

        // ── Vũ khí đang trang bị ──────────────────────────────────────
        inventory.Add(new Inventory
        {
            inventoryId = Guid.NewGuid().ToString("N"),
            characterId = CurrentCharacter.characterId,
            itemId      = "item_rusty_sword",
            quantity    = 1,
            equipped    = true,
            slotIndex   = 0,
            locked      = false,
            acquiredAt  = DateTime.UtcNow.AddDays(-5)
        });

        // ── Các item CHƯA trang bị (để test lưới inventory bên phải) ──
        var seedItems = new[]
        {
            // Weapon
            ("item_steel_dagger",    1, false),
            ("item_shadow_blade",    1, false),
            ("item_excalibur",       1, false),
            // Armor
            ("item_leather_vest",    1, false),
            ("item_iron_shield",     1, false),
            ("item_dragon_scale",    1, false),
            ("item_aegis",           1, false),
            // Accessory
            ("item_wooden_ring",     1, false),
            ("item_silver_amulet",   1, false),
            ("item_void_ring",       1, false),
            ("item_ring_of_gods",    1, false),
            // Consumable (stackable)
            ("item_health_potion_s", 5, false),
            ("item_health_potion_m", 3, false),
            ("item_elixir",          2, false),
            ("item_divine_elixir",   1, false),
        };

        int slot = 1;
        foreach (var (itemId, qty, eq) in seedItems)
        {
            inventory.Add(new Inventory
            {
                inventoryId = Guid.NewGuid().ToString("N"),
                characterId = CurrentCharacter.characterId,
                itemId      = itemId,
                quantity    = qty,
                equipped    = eq,
                slotIndex   = slot++,
                locked      = false,
                acquiredAt  = DateTime.UtcNow.AddDays(-new System.Random().Next(0, 5))
            });
        }

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

        // Seed mock titles
        titles.Clear();
        titles.Add(new CharacterTitle
        {
            titleId     = Guid.NewGuid().ToString("N"),
            characterId = CurrentCharacter.characterId,
            name        = "Kẻ Tiêu Diệt Bóng Tối",
            description = "Hạ gục Shadow Demon lần đầu tiên",
            rarity      = "Rare",
            isEquipped  = true,
            earnedAt    = DateTime.UtcNow.AddDays(-1)
        });
        titles.Add(new CharacterTitle
        {
            titleId     = Guid.NewGuid().ToString("N"),
            characterId = CurrentCharacter.characterId,
            name        = "Kẻ Lạc Đường",
            description = "Đặt chân vào Ancient Ruins",
            rarity      = "Common",
            isEquipped  = false,
            earnedAt    = DateTime.UtcNow.AddDays(-3)
        });
        titles.Add(new CharacterTitle
        {
            titleId     = Guid.NewGuid().ToString("N"),
            characterId = CurrentCharacter.characterId,
            name        = "Sinh Tồn Thần Kỳ",
            description = "Kết thúc trận đấu với HP còn dưới 10%",
            rarity      = "Epic",
            isEquipped  = false,
            earnedAt    = DateTime.UtcNow.AddHours(-6)
        });

        // Seed mock battle history
        for (int i = 0; i < 3; i++)
        {
            string encId  = Guid.NewGuid().ToString("N");
            string battleId = Guid.NewGuid().ToString("N");
            bool victory  = i != 1; // lần 2 (index 1) thua

            BossEncounter mockEnc = new BossEncounter
            {
                encounterId   = encId,
                characterId   = CurrentCharacter.characterId,
                bossId        = CurrentBoss.bossId,
                bossLevel     = CurrentBoss.level,
                bossRarity    = CurrentBoss.rarity,
                playerHpBefore = CurrentCharacter.maxHp,
                playerHpAfter  = victory ? Mathf.Max(5, CurrentCharacter.maxHp - 36) : 0,
                bossHpBefore   = CurrentBoss.baseHp,
                bossHpAfter    = victory ? 0 : Mathf.Max(10, CurrentBoss.baseHp - 80),
                status         = victory ? "Victory" : "Defeat",
                encounterTime  = DateTime.UtcNow.AddHours(-(i + 1) * 2)
            };
            encounters.Add(mockEnc);

            battles.Add(new Battle
            {
                battleId           = battleId,
                encounterId        = encId,
                playerPower        = CurrentCharacter.attack + CurrentCharacter.level * 2,
                bossPower          = CurrentBoss.baseAttack,
                battleType         = "Boss",
                status             = "Completed",
                result             = victory ? "Victory" : "Defeat",
                turnCount          = victory ? 4 : 6,
                durationMs         = victory ? 4800 : 7200,
                playerSnapshotJson = "{}",
                bossSnapshotJson   = "{}",
                rewardJson         = victory ? "{\"gold\":100,\"exp\":75}" : "{}",
                battleTime         = mockEnc.encounterTime
            });
        }
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
            CurrentCharacter.attack += 3;
            CurrentCharacter.defense += 2;
            requiredExperience = CurrentCharacter.level * 100;
        }
    }

    public void AddItemToInventory(string itemId, int quantity = 1, bool equipped = false)
    {
        if (inventory == null) return;

        var template = GameShared.Config.GameConstants.GetItemById(itemId);
        bool isStackable = template != null && template.stackable;

        // Chỉ cộng dồn với các vật phẩm có tính chất stackable (như Thuốc hồi máu)
        if (isStackable)
        {
            Inventory existing = inventory.Find(entry => entry.itemId == itemId && entry.equipped == equipped);
            if (existing != null)
            {
                existing.quantity += quantity;
                return;
            }
        }

        // Các trang bị (Weapon, Armor, Accessory) luôn có mã inventoryId duy nhất cho từng món
        inventory.Add(new Inventory
        {
            inventoryId = Guid.NewGuid().ToString("N"),
            characterId = CurrentCharacter != null ? CurrentCharacter.characterId : "local_char",
            itemId = itemId,
            quantity = quantity,
            equipped = equipped,
            slotIndex = inventory.Count,
            locked = false,
            acquiredAt = DateTime.UtcNow
        });
    }

    private int nextAccessorySlotToReplace = 1; // 1 hoặc 2 (Dùng xoay vòng khi thay thế trang sức khi đã trang bị đủ 2 món)

    public bool IsItemEquipped(string itemId)
    {
        if (inventory == null) return false;
        var item = inventory.Find(i => i.itemId.Equals(itemId, StringComparison.OrdinalIgnoreCase));
        return item != null && item.equipped;
    }

    public bool ToggleEquipItem(string identifier)
    {
        return ToggleEquipItemByInventoryId(identifier);
    }

    public bool ToggleEquipItemByInventoryId(string inventoryIdOrItemId)
    {
        if (inventory == null) return false;
        // Tìm đúng bản ghi theo inventoryId duy nhất, nếu không thấy thì fallback tìm theo itemId
        var targetItem = inventory.Find(i => i.inventoryId == inventoryIdOrItemId) 
                      ?? inventory.Find(i => i.itemId.Equals(inventoryIdOrItemId, StringComparison.OrdinalIgnoreCase));
        if (targetItem == null) return false;

        string itemId = targetItem.itemId;
        ItemType itemType = ItemData.GetItemTypeFromId(itemId);
        var template = GameShared.Config.GameConstants.GetItemById(itemId);
        if (template != null && Enum.TryParse<ItemType>(template.itemType, true, out var parsedType))
        {
            itemType = parsedType;
        }

        // 1. NẾU VẬT PHẨM ĐANG ĐƯỢC TRANG BỊ -> THÁO TRANG BỊ (UNEQUIP)
        if (targetItem.equipped)
        {
            targetItem.equipped = false;
            RecalculateCharacterStats();
            Debug.Log($"🛡️ [UNEQUIP] Đã tháo vật phẩm '{itemId}' (ID={targetItem.inventoryId}, {itemType}).");
            return false;
        }

        // 2. NẾU VẬT PHẨM CHƯA ĐƯỢC TRANG BỊ -> TRANG BỊ HỢP LỆ THEO QUY TẮC GIỚI HẠN:
        if (itemType == ItemType.Weapon || itemType == ItemType.Armor)
        {
            foreach (var inv in inventory)
            {
                if (inv.equipped && inv.inventoryId != targetItem.inventoryId)
                {
                    ItemType currentType = ItemData.GetItemTypeFromId(inv.itemId);
                    var t = GameShared.Config.GameConstants.GetItemById(inv.itemId);
                    if (t != null && Enum.TryParse<ItemType>(t.itemType, true, out var pt)) currentType = pt;

                    if (currentType == itemType)
                    {
                        inv.equipped = false; // Tự động tháo trang bị cũ cùng loại!
                        if (CurrentCharacter != null && !string.IsNullOrEmpty(CurrentCharacter.characterId) && !string.IsNullOrEmpty(inv.inventoryId))
                        {
                            _ = ApiClient.Instance?.PostAsync<object>($"inventory/{CurrentCharacter.characterId}/unequip", new GameShared.DTOs.Inventory.UnequipItemRequest { inventoryId = inv.inventoryId });
                        }
                        Debug.Log($"🔄 [AUTO UNEQUIP] Tự động tháo '{inv.itemId}' (ID={inv.inventoryId}, {itemType}) cũ để nhường chỗ cho '{itemId}'.");
                    }
                }
            }

            targetItem.equipped = true;
        }
        else if (itemType == ItemType.Accessory)
        {
            // Tự động tháo phụ kiện cũ (nếu có) — UI chỉ có 1 ô Accessory
            foreach (var inv in inventory)
            {
                if (inv.equipped && inv.inventoryId != targetItem.inventoryId)
                {
                    ItemType currentType = ItemData.GetItemTypeFromId(inv.itemId);
                    var t = GameShared.Config.GameConstants.GetItemById(inv.itemId);
                    if (t != null && Enum.TryParse<ItemType>(t.itemType, true, out var pt)) currentType = pt;

                    if (currentType == ItemType.Accessory)
                    {
                        inv.equipped = false;
                        Debug.Log($"🔄 [AUTO UNEQUIP] Tự động tháo phụ kiện '{inv.itemId}' (ID={inv.inventoryId}) để nhường chỗ cho '{itemId}'.");
                    }
                }
            }
            targetItem.equipped = true;
            Debug.Log($"💍 [EQUIP ACCESSORY] Đã trang bị phụ kiện '{itemId}' (ID={targetItem.inventoryId}).");
        }
        else
        {
            targetItem.equipped = true;
        }

        RecalculateCharacterStats();

        // Gửi API đồng bộ trạng thái trang bị lên AWS DynamoDB
        if (CurrentCharacter != null && !string.IsNullOrEmpty(CurrentCharacter.characterId) && !string.IsNullOrEmpty(targetItem.inventoryId))
        {
            string charId = CurrentCharacter.characterId;
            string invId = targetItem.inventoryId;
            if (targetItem.equipped)
            {
                _ = ApiClient.Instance?.PostAsync<object>($"inventory/{charId}/equip", new GameShared.DTOs.Inventory.EquipItemRequest { inventoryId = invId, itemId = targetItem.itemId });
            }
            else
            {
                _ = ApiClient.Instance?.PostAsync<object>($"inventory/{charId}/unequip", new GameShared.DTOs.Inventory.UnequipItemRequest { inventoryId = invId, itemId = targetItem.itemId });
            }
        }

        Debug.Log($"⚔️ [EQUIP SYSTEM] Đã trang bị '{itemId}' (ID={targetItem.inventoryId}, {itemType}) thành công! Sức mạnh mới của {CurrentCharacter.name}: Attack={CurrentCharacter.attack}, Defense={CurrentCharacter.defense}, MaxHP={CurrentCharacter.maxHp}");
        return targetItem.equipped;
    }

    public List<string> GetEquippedInventoryItemIds()
    {
        var list = new List<string>();
        if (inventory == null) return list;
        foreach (var inv in inventory)
        {
            if (inv.equipped && !string.IsNullOrEmpty(inv.itemId))
            {
                list.Add(inv.itemId);
            }
        }
        return list;
    }

    public void RecalculateCharacterStats()
    {
        if (CurrentCharacter == null || inventory == null) return;

        int baseHp = 100 + (CurrentCharacter.level - 1) * 12;
        int baseAtk = 10 + (CurrentCharacter.level - 1) * 3;
        int baseDef = 5 + (CurrentCharacter.level - 1) * 2;

        int bonusAtk = 0;
        int bonusDef = 0;
        int bonusHp = 0;

        foreach (var inv in inventory)
        {
            if (inv.equipped)
            {
                var template = GameShared.Config.GameConstants.GetItemById(inv.itemId);
                if (template != null)
                {
                    bonusAtk += template.attackBonus;
                    bonusDef += template.defenseBonus;
                    bonusHp += template.hpBonus;
                }
                else
                {
                    ItemType type = ItemData.GetItemTypeFromId(inv.itemId);
                    if (type == ItemType.Weapon) bonusAtk += 5;
                    else if (type == ItemType.Armor) bonusDef += 5;
                    else if (type == ItemType.Accessory) { bonusAtk += 2; bonusDef += 2; bonusHp += 10; }
                }
            }
        }

        int oldMaxHp = CurrentCharacter.maxHp;
        CurrentCharacter.maxHp = baseHp + bonusHp;
        CurrentCharacter.attack = baseAtk + bonusAtk;
        CurrentCharacter.defense = baseDef + bonusDef;

        // Nếu Max HP tăng lên nhờ mặc đồ, tự động hồi Máu hiện tại theo đúng lượng tăng!
        if (CurrentCharacter.maxHp > oldMaxHp)
        {
            CurrentCharacter.hp = Mathf.Min(CurrentCharacter.maxHp, CurrentCharacter.hp + (CurrentCharacter.maxHp - oldMaxHp));
        }
        else
        {
            CurrentCharacter.hp = Mathf.Min(CurrentCharacter.hp, CurrentCharacter.maxHp);
        }
    }
}