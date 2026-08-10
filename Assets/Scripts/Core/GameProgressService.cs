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

    /// <summary>
    /// Nếu true: lần gọi story/start tiếp theo sẽ xóa session cũ và bắt đầu lại từ đầu.
    /// Được set khi người chơi chết và chọn Back to Menu.
    /// </summary>
    public bool ShouldForceNewSession { get; set; } = false;

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

        bool useMock = GameConfigSO.Instance != null && GameConfigSO.Instance.useMockMode;
        if (useMock)
        {
            SeedMockWorld();
            RecalculateCharacterStats();
        }
        else
        {
            // Online mode: Chỉ khởi tạo dữ liệu trống, chờ AuthManager cập nhật từ AWS
            if (CurrentUser == null)
            {
                CurrentUser = new User
                {
                    userId = "",
                    username = "",
                    displayName = "",
                    status = "Active"
                };
            }
            if (CurrentCharacter == null)
            {
                string savedCharId = PlayerPrefs.GetString("lastCharacterId", "");
                CurrentCharacter = new Character
                {
                    characterId = !string.IsNullOrEmpty(savedCharId) ? savedCharId : "demo_char_id",
                    name = "Khoi",
                    level = 1,
                    hp = 100,
                    maxHp = 100,
                    attack = 15,
                    defense = 5,
                    gold = 50,
                    className = "Adventurer",
                    status = "Alive"
                };
            }

        }
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
        string name = !string.IsNullOrEmpty(user.displayName) ? user.displayName : user.username;
        if (CurrentCharacter != null && !string.IsNullOrEmpty(name))
        {
            CurrentCharacter.name = name;
        }
        if (name != null && name.Equals("khoi", StringComparison.OrdinalIgnoreCase))
        {
            if (CurrentCharacter != null) CurrentCharacter.gold = 999999;
        }
        Debug.Log($"[GameProgressService] CurrentUser set: {name} (id={user.userId})");
    }

    /// <summary>
    /// Cập nhật CurrentCharacter sau khi tạo hoặc tải nhân vật từ API backend.
    /// </summary>
    public void SetCurrentCharacter(Character character)
    {
        if (character == null) return;
        CurrentCharacter = character;
        if (character.name != null && character.name.Equals("khoi", StringComparison.OrdinalIgnoreCase))
        {
            CurrentCharacter.gold = 999999;
        }
        Debug.Log($"[GameProgressService] CurrentCharacter set: {character.name} (id={character.characterId})");
    }

    /// <summary>
    /// Đồng bộ chỉ số nhân vật (Level, HP, MaxHP, Gold, ATK, DEF) từ response của Backend API.
    /// </summary>
    public void SyncCharacterFromResponse(GameShared.DTOs.Character.CharacterResponse res)
    {
        if (res == null || CurrentCharacter == null) return;

        CurrentCharacter.level = res.level;
        CurrentCharacter.experience = res.experience;
        CurrentCharacter.hp = res.hp;
        CurrentCharacter.maxHp = res.maxHp;
        CurrentCharacter.attack = res.attack;
        CurrentCharacter.defense = res.defense;
        if (CurrentCharacter.name != null && !CurrentCharacter.name.Equals("khoi", StringComparison.OrdinalIgnoreCase))
        {
            CurrentCharacter.gold = res.gold;
        }
        Debug.Log($"[GameProgressService] Synced Character from backend: {CurrentCharacter.name} (Lv.{CurrentCharacter.level}, Exp={CurrentCharacter.experience}, HP={CurrentCharacter.hp}/{CurrentCharacter.maxHp}, Gold={CurrentCharacter.gold})");
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

    /// <summary>Xóa session khi logout. Không reset initialized để tránh SeedMockWorld ghi đè khi login lại.</summary>
    public void ClearUser()
    {
        CurrentUser = null;
        CurrentCharacter = null;
        CurrentStorySession = null;
        CurrentBoss = null;
        inventory.Clear();
        Debug.Log("[GameProgressService] Session cleared.");
    }

    /// <summary>
    /// Hồi sinh nhân vật bằng Vàng (50 Gold) và đưa máu về maxHP.
    /// </summary>
    public bool ReviveCharacterWithGold(int goldCost = 50)
    {
        if (CurrentCharacter == null) return false;

        if (CurrentCharacter.gold < goldCost)
        {
            Debug.LogWarning($"[GameProgressService] Không đủ vàng để hồi sinh. Cần {goldCost} Gold, hiện có {CurrentCharacter.gold} Gold.");
            return false;
        }

        CurrentCharacter.gold -= goldCost;
        CurrentCharacter.status = "Alive";
        CurrentCharacter.hp = CurrentCharacter.maxHp > 0 ? CurrentCharacter.maxHp : 100;

        Debug.Log($"[GameProgressService] Nhân vật {CurrentCharacter.name} đã được hồi sinh với 100% HP! (Trừ {goldCost} Gold, còn lại {CurrentCharacter.gold} Gold)");
        return true;
    }

    /// <summary>
    /// Reset toàn bộ tiến trình game khi người chơi chấp nhận thua (Chết luôn), bắt đầu lại game mới từ đầu.
    /// </summary>
    public void ResetGameProgressToStartNew()
    {
        if (CurrentCharacter != null)
        {
            int defaultGold = (CurrentCharacter.name != null && CurrentCharacter.name.Equals("khoi", StringComparison.OrdinalIgnoreCase)) ? 999999 : 50;
            CurrentCharacter.level = 1;
            CurrentCharacter.experience = 0;
            CurrentCharacter.hp = 100;
            CurrentCharacter.maxHp = 100;
            CurrentCharacter.attack = 15;
            CurrentCharacter.defense = 5;
            CurrentCharacter.gold = defaultGold;
            CurrentCharacter.status = "Alive";
            CurrentCharacter.currentLocationId = "ancient_cave";
        }

        if (CurrentStorySession != null)
        {
            CurrentStorySession.currentChapterId = "chapter_1";
            CurrentStorySession.currentNodeId = "chapter_1";
            CurrentStorySession.currentLocation = "ancient_cave";
            CurrentStorySession.storySummary = "Mở đầu cuộc phiêu lưu tại Hang Động Cổ Đại.";
            CurrentStorySession.status = "Active";
        }

        CurrentBoss = null;
        inventory.Clear();
        SeedDefaultInventoryIfNeeded();

        ShouldForceNewSession = true; // Đánh dấu để story/start tiếp theo sẽ xóa session cũ trên server

        Debug.Log("[GameProgressService] Đã reset toàn bộ tiến trình game về trạng thái khởi đầu mới (Chương 1). ShouldForceNewSession = true.");
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

        // ── Tính Boss Level đồng bộ với Backend ─────
        int bossLevel = GameShared.Config.GameConstants.CalculateBossLevel(playerLevel, selectedRarity);

        CurrentBoss = new Boss
        {
            bossId       = picked.bossId,  // Dùng đúng bossId của GameConstants
            name         = picked.name,
            rarity       = picked.rarity,
            level        = bossLevel,
            baseHp       = GameShared.Config.GameConstants.ScaleStat(picked.baseHp, bossLevel),
            baseAttack   = GameShared.Config.GameConstants.ScaleStat(picked.baseAttack, bossLevel),
            baseDefense  = GameShared.Config.GameConstants.ScaleStat(picked.baseDefense, bossLevel),
            speed        = picked.speed,
            criticalRate = picked.criticalRate,
            expReward    = picked.expReward,
            goldReward   = picked.goldReward,
            skillSetJson = "[]",
            imageUrl     = string.Empty
        };

        Debug.Log($"[GameProgressService] SpawnRandomBoss: {CurrentBoss.name} (bossId={CurrentBoss.bossId}, Rarity={CurrentBoss.rarity}, Level={CurrentBoss.level}, HP={CurrentBoss.baseHp})");
    }

    /// <summary>
    /// Sinh Boss chính xác theo bossId do AI Bedrock chỉ định, cho phép gán Level trực tiếp từ AI.
    /// </summary>
    public void SpawnBossById(string bossId, int? targetLevel = null)
    {
        InitializeIfNeeded();

        if (string.IsNullOrWhiteSpace(bossId))
        {
            SpawnRandomBoss();
            return;
        }

        var cleanId = bossId.StartsWith("boss_") ? bossId[5..] : bossId;

        // Tìm Boss template trong GameConstants.BossCatalog khớp với bossId từ AI Bedrock
        Boss picked = GameShared.Config.GameConstants.BossCatalog.FirstOrDefault(b =>
            b != null && (
                b.bossId.Equals(bossId, StringComparison.OrdinalIgnoreCase) ||
                b.bossId.Equals($"boss_{cleanId}", StringComparison.OrdinalIgnoreCase) ||
                b.bossId.EndsWith(cleanId, StringComparison.OrdinalIgnoreCase)
            )
        );

        if (picked == null)
        {
            Debug.LogWarning($"[GameProgressService] Không tìm thấy bossId '{bossId}' trong BossCatalog, fallback ngẫu nhiên.");
            SpawnRandomBoss();
            return;
        }

        int playerLevel = CurrentCharacter != null ? CurrentCharacter.level : 1;
        int bossLevel = (targetLevel.HasValue && targetLevel.Value > 0)
            ? targetLevel.Value
            : GameShared.Config.GameConstants.CalculateBossLevel(playerLevel, picked.rarity, picked.bossId);

        CurrentBoss = new Boss
        {
            bossId       = picked.bossId,
            name         = picked.name,
            rarity       = picked.rarity,
            level        = bossLevel,
            baseHp       = GameShared.Config.GameConstants.ScaleStat(picked.baseHp, bossLevel),
            baseAttack   = GameShared.Config.GameConstants.ScaleStat(picked.baseAttack, bossLevel),
            baseDefense  = GameShared.Config.GameConstants.ScaleStat(picked.baseDefense, bossLevel),
            speed        = picked.speed,
            criticalRate = picked.criticalRate,
            expReward    = GameShared.Config.GameConstants.CalculateExpReward(bossLevel, picked.rarity, playerLevel),
            goldReward   = GameShared.Config.GameConstants.CalculateGoldReward(bossLevel, picked.rarity),
            skillSetJson = "[]",
            imageUrl     = string.Empty
        };

        Debug.Log($"[GameProgressService] SpawnBossById: {CurrentBoss.name} (bossId={CurrentBoss.bossId}, Level={CurrentBoss.level}, HP={CurrentBoss.baseHp}, ATK={CurrentBoss.baseAttack}, DEF={CurrentBoss.baseDefense})");
    }



    public StoryData CreateStoryDemoData()
    {
        InitializeIfNeeded();

        StoryCharacterState characterState = new StoryCharacterState
        {
            characterName = CurrentCharacter.name,
            level = CurrentCharacter.level,
            hp = CurrentCharacter.hp,
            gold = CurrentCharacter.gold,
            xp = CurrentCharacter.experience,
            maxXP = CurrentCharacter.level * 100
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
            gold = CurrentCharacter.gold,
            xp = CurrentCharacter.experience,
            maxXP = CurrentCharacter.level * 100
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
            int goldEarned = GameShared.Config.GameConstants.CalculateGoldReward(CurrentBoss?.level ?? 1, CurrentBoss?.rarity ?? "Common");
            int expEarned = GameShared.Config.GameConstants.CalculateExpReward(CurrentBoss?.level ?? 1, CurrentBoss?.rarity ?? "Common", CurrentCharacter?.level ?? 1);
            CurrentCharacter.gold += goldEarned;
            CurrentCharacter.experience += expEarned;
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

    public void SeedDefaultInventoryIfNeeded()
    {
        if (inventory.Count > 0) return;

        string charId = CurrentCharacter != null ? CurrentCharacter.characterId : "default_char";

        inventory.Add(new Inventory
        {
            inventoryId = Guid.NewGuid().ToString("N"),
            characterId = charId,
            itemId      = "item_rusty_sword",
            quantity    = 1,
            equipped    = true,
            slotIndex   = 0,
            locked      = false,
            acquiredAt  = DateTime.UtcNow
        });

        var seedItems = new (string, int, bool)[0];

        int slot = 1;
        foreach (var (itemId, qty, eq) in seedItems)
        {
            inventory.Add(new Inventory
            {
                inventoryId = Guid.NewGuid().ToString("N"),
                characterId = charId,
                itemId      = itemId,
                quantity    = qty,
                equipped    = eq,
                slotIndex   = slot++,
                locked      = false,
                acquiredAt  = DateTime.UtcNow
            });
        }
    }

    public IReadOnlyList<Inventory> GetInventory()
    {
        if (inventory.Count == 0)
        {
            SeedDefaultInventoryIfNeeded();
        }
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
        var seedItems = new (string, int, bool)[0];

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
        
        // Mặc định đọc từ GameConstants. Nếu không có template, tự động cho phép stack nếu là Consumable
        bool isStackable = template != null ? template.stackable : (ItemData.GetItemTypeFromId(itemId) == ItemType.Consumable);

        // NẾU BẠN MUỐN TẤT CẢ VẬT PHẨM (KỂ CẢ VŨ KHÍ, GIÁP) ĐỀU CỘNG DỒN SỐ LƯỢNG KHI RƠI RA,
        // HÃY BỎ COMMENT DÒNG BÊN DƯỚI ĐỂ ÉP BUỘC LUÔN STACK:
        isStackable = true; 

        // Chỉ cộng dồn với các vật phẩm có tính chất stackable (như Thuốc hồi máu, hoặc nếu bạn bật true ở trên)
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

    private void UnequipAndMergeItem(Inventory itemToUnequip)
    {
        itemToUnequip.equipped = false;
        // Thử tìm trong túi đồ xem có đống nào cùng loại chưa trang bị không để gộp vào
        var existingStack = inventory.Find(i => i.itemId == itemToUnequip.itemId && !i.equipped && i.inventoryId != itemToUnequip.inventoryId);
        if (existingStack != null)
        {
            existingStack.quantity += itemToUnequip.quantity;
            inventory.Remove(itemToUnequip); // Hủy cái bị tháo ra vì đã gộp xong
        }
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

        // 1. NẾU VẬT PHẨM ĐANG ĐƯỢC TRANG BỊ -> THÁO TRANG BỊ (UNEQUIP) VÀ GỘP VÀO TÚI
        if (targetItem.equipped)
        {
            UnequipAndMergeItem(targetItem);
            RecalculateCharacterStats();
            Debug.Log($"🛡️ [UNEQUIP] Đã tháo vật phẩm '{itemId}' (Và gộp số lượng nếu có).");
            return false;
        }

        // 2. NẾU VẬT PHẨM CHƯA ĐƯỢC TRANG BỊ -> TRANG BỊ HỢP LỆ THEO QUY TẮC GIỚI HẠN:
        // TÁCH ITEM NẾU SỐ LƯỢNG LỚN HƠN 1
        if (targetItem.quantity > 1)
        {
            targetItem.quantity -= 1; // Bớt 1 món trong đống đang có ở túi
            
            // Tạo ra 1 món đồ mới tinh (clone) số lượng 1 để mặc lên người
            var equippedCopy = new Inventory
            {
                inventoryId = Guid.NewGuid().ToString("N"),
                characterId = targetItem.characterId,
                itemId = targetItem.itemId,
                quantity = 1,
                equipped = false, // Lát nữa ở dưới sẽ set true
                slotIndex = targetItem.slotIndex,
                locked = targetItem.locked,
                acquiredAt = DateTime.UtcNow
            };
            inventory.Add(equippedCopy);
            targetItem = equippedCopy; // Trỏ con trỏ sang món đồ mới cắt ra để xử lý phần dưới
        }

        if (itemType == ItemType.Weapon || itemType == ItemType.Armor)
        {
            // Do List sẽ bị thay đổi kích thước nếu UnequipAndMergeItem hủy item cũ, ta dùng danh sách copy để duyệt an toàn
            var currentInventoryCopy = new List<Inventory>(inventory);
            foreach (var inv in currentInventoryCopy)
            {
                if (inv.equipped && inv.inventoryId != targetItem.inventoryId)
                {
                    ItemType currentType = ItemData.GetItemTypeFromId(inv.itemId);
                    var t = GameShared.Config.GameConstants.GetItemById(inv.itemId);
                    if (t != null && Enum.TryParse<ItemType>(t.itemType, true, out var pt)) currentType = pt;

                    if (currentType == itemType)
                    {
                        if (CurrentCharacter != null && !string.IsNullOrEmpty(CurrentCharacter.characterId) && !string.IsNullOrEmpty(inv.inventoryId))
                        {
                            _ = ApiClient.Instance?.PostAsync<object>($"inventory/{CurrentCharacter.characterId}/unequip", new GameShared.DTOs.Inventory.UnequipItemRequest { inventoryId = inv.inventoryId });
                        }
                        Debug.Log($"🔄 [AUTO UNEQUIP] Tự động tháo '{inv.itemId}' cũ để nhường chỗ cho '{itemId}'.");
                        UnequipAndMergeItem(inv); // Tháo đồ cũ và gộp vô túi
                    }
                }
            }

            targetItem.equipped = true;
        }
        else if (itemType == ItemType.Accessory)
        {
            // Tự động tháo phụ kiện cũ (nếu có) — UI chỉ có 1 ô Accessory
            var currentInventoryCopy = new List<Inventory>(inventory);
            foreach (var inv in currentInventoryCopy)
            {
                if (inv.equipped && inv.inventoryId != targetItem.inventoryId)
                {
                    ItemType currentType = ItemData.GetItemTypeFromId(inv.itemId);
                    var t = GameShared.Config.GameConstants.GetItemById(inv.itemId);
                    if (t != null && Enum.TryParse<ItemType>(t.itemType, true, out var pt)) currentType = pt;

                    if (currentType == ItemType.Accessory)
                    {
                        Debug.Log($"🔄 [AUTO UNEQUIP] Tự động tháo phụ kiện '{inv.itemId}' để nhường chỗ cho '{itemId}'.");
                        UnequipAndMergeItem(inv);
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