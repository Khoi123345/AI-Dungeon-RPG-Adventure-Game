using GameBackend.Core.Config;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameShared.DTOs.Battle;
using GameShared.DTOs.Story;
using GameBackend.Core.Services.Parsing;
using GameShared.DTOs.Inventory;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
    public class BattleService : IBattleService
    {
        private readonly IBossRepository _bossRepository;
        private readonly IBattleRepository _battleRepository;
        private readonly ICharacterRepository _characterRepository;
        private readonly ICharacterService _characterService;
        private readonly IInventoryService _inventoryService;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly IDefeatedBossRepository _defeatedBossRepository;
        private readonly IStoryRepository _storyRepository;
        private readonly ILogger<BattleService> _logger;
        private readonly Random _random = new();

        public BattleService(
            IBossRepository bossRepository,
            IBattleRepository battleRepository,
            ICharacterRepository characterRepository,
            ICharacterService characterService,
            IInventoryService inventoryService,
            IInventoryRepository inventoryRepository,
            IDefeatedBossRepository defeatedBossRepository,
            IStoryRepository storyRepository,
            ILogger<BattleService> logger)
        {
            _bossRepository = bossRepository;
            _battleRepository = battleRepository;
            _characterRepository = characterRepository;
            _characterService = characterService;
            _inventoryService = inventoryService;
            _inventoryRepository = inventoryRepository;
            _defeatedBossRepository = defeatedBossRepository;
            _storyRepository = storyRepository;
            _logger = logger;
        }

        // =====================================================================
        // SPAWN BOSS (Mục 3 logic doc)
        // =====================================================================

        public async Task<BossSpawnResponse> SpawnBossAsync(BossSpawnRequest request)
        {
            var character = await _characterRepository.GetByIdAsync(request.characterId)
                ?? throw new Utils.GameNotFoundException("Character not found");

            if (character.status == "Dead")
            {
                throw new Utils.GameValidationException("Cannot spawn boss while character is dead");
            }

            // 1. Nếu có encounterId được truyền vào từ Cốt truyện, thử lấy encounter đang active
            BossEncounter? existingEncounter = null;
            if (!string.IsNullOrWhiteSpace(request.encounterId))
            {
                existingEncounter = await _battleRepository.GetEncounterByIdAsync(request.encounterId);
            }

            // 2. Tìm Boss template phù hợp từ Cốt truyện (nếu có request.bossId hoặc từ existingEncounter)
            string targetBossId = !string.IsNullOrWhiteSpace(request.bossId)
                ? request.bossId
                : existingEncounter?.bossId ?? string.Empty;

            string cleanTarget = (targetBossId ?? "").Trim().ToLowerInvariant();
            string strippedTarget = cleanTarget;
            if (strippedTarget.StartsWith("mob_")) strippedTarget = strippedTarget[4..];
            if (strippedTarget.StartsWith("boss_")) strippedTarget = strippedTarget[5..];

            Boss? template = FindBossTemplate(targetBossId);


            string rarity = template != null ? template.rarity : "Common";
            if (template == null)
            {
                // Nếu là mob thường chưa có trong Catalog (ví dụ mob_cave_bat), tự tạo template Mob thường nhẹ nhàng thay vì fallback sang Boss lớn (Shadow Demon)
                var minorMobs = GameConstants.BossCatalog.Where(b => b.bossId.StartsWith("mob_")).ToList();
                var fallbackBase = minorMobs.Count > 0
                    ? minorMobs[_random.Next(minorMobs.Count)]
                    : new Boss { baseHp = 25, baseAttack = 5, baseDefense = 1, speed = 8, criticalRate = 0.03f, expReward = 10, goldReward = 8 };

                string mobTitleName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(strippedTarget.Replace("_", " "));

                template = new Boss
                {
                    bossId = targetBossId,
                    name = mobTitleName,
                    rarity = "Common",
                    baseHp = fallbackBase.baseHp,
                    baseAttack = fallbackBase.baseAttack,
                    baseDefense = fallbackBase.baseDefense,
                    speed = fallbackBase.speed,
                    criticalRate = fallbackBase.criticalRate,
                    expReward = fallbackBase.expReward,
                    goldReward = fallbackBase.goldReward
                };
            }


            int bossLevel = request.bossLevel > 0
                ? request.bossLevel
                : (existingEncounter != null && existingEncounter.bossLevel > 0
                    ? existingEncounter.bossLevel
                    : GameConstants.CalculateBossLevel(character.level, rarity));

            string actualBossId = !string.IsNullOrWhiteSpace(targetBossId)
                ? targetBossId
                : $"{template.bossId}_{Guid.NewGuid().ToString("N")[..8]}";

            var encounter = existingEncounter ?? new BossEncounter
            {
                encounterId    = Guid.NewGuid().ToString("N"),
                characterId    = character.characterId,
                bossId         = actualBossId,
                bossLevel      = bossLevel,
                bossRarity     = rarity,
                playerHpBefore = character.hp,
                bossHpBefore   = GameConstants.ScaleStat(template.baseHp, bossLevel),
                status         = "Active",
                encounterTime  = DateTime.UtcNow
            };

            if (existingEncounter != null)
            {
                existingEncounter.bossId         = actualBossId;
                existingEncounter.bossLevel      = bossLevel;
                existingEncounter.bossRarity     = rarity;
                existingEncounter.bossHpBefore   = GameConstants.ScaleStat(template.baseHp, bossLevel);
                existingEncounter.playerHpBefore = character.hp;
                existingEncounter.encounterTime  = DateTime.UtcNow;
                await _battleRepository.SaveEncounterAsync(existingEncounter);
            }
            else
            {
                await _battleRepository.SaveEncounterAsync(encounter);
            }


            _logger.LogInformation("Boss spawned: {BossId} ({BossName}, Lv.{Level}, {Rarity}) for character: {CharacterId}",
                encounter.bossId, template.name, bossLevel, rarity, character.characterId);

            return new BossSpawnResponse
            {
                encounterId   = encounter.encounterId,
                bossId        = encounter.bossId,
                bossName      = template.name,
                bossRarity    = rarity,
                bossLevel     = bossLevel,
                bossHp        = GameConstants.ScaleStat(template.baseHp, bossLevel),
                bossAttack    = GameConstants.ScaleStat(template.baseAttack, bossLevel),
                bossDefense   = GameConstants.ScaleStat(template.baseDefense, bossLevel),
                bossSpeed     = template.speed,
                bossCriticalRate = template.criticalRate,
                bossImageUrl  = template.imageUrl ?? ""
            };
        }

        // =====================================================================
        // RESOLVE BATTLE — Battle Score Formula (Mục 4 logic doc)
        // =====================================================================

        public async Task<BattleResolveResponse> ResolveBattleAsync(BattleResolveRequest request)
        {
            // 1. Lấy Character và Encounter
            var character = await _characterRepository.GetByIdAsync(request.characterId)
                ?? throw new Utils.GameNotFoundException("Character not found");

            var encounter = await _battleRepository.GetEncounterByIdAsync(request.encounterId)
                ?? throw new Utils.GameNotFoundException("Encounter not found");

            if (encounter.status != "Active")
            {
                throw new Utils.GameValidationException("Encounter is no longer active");
            }

            // 2. Tính Player Power (Mục 4)
            //    Player Power = Attack(Total) + Defense * 0.5 + HP * 0.05 + Level Bonus
            //    Thêm defense và hp vào để cân bằng với Boss Power (vốn đã bao gồm baseDefense)
            var equippedItems = await _inventoryRepository.GetEquippedItemsAsync(character.characterId);
            if ((equippedItems == null || equippedItems.Count == 0) && request.equippedItemIds != null && request.equippedItemIds.Count > 0)
            {
                equippedItems = request.equippedItemIds.Select(id => new Inventory
                {
                    inventoryId = Guid.NewGuid().ToString("N"),
                    characterId = character.characterId,
                    itemId = id,
                    equipped = true
                }).ToList();
            }
            var itemLookup = BuildItemLookup(equippedItems);
            var effectiveStats = _characterService.CalculateEffectiveStats(character, equippedItems, itemLookup);

            double playerPower = effectiveStats.attack
                               + effectiveStats.defense * 0.5
                               + effectiveStats.maxHp   * 0.05
                               + character.level * 2;

            // 3. Tính Boss Power (Mục 4)
            //    Boss Power = Base Attack × (1 + Level × 0.1) + Base Defense + Level Modifier
            string targetBossId = encounter.bossId ?? "";
            string cleanTarget = targetBossId.Trim().ToLowerInvariant();
            string strippedTarget = cleanTarget;
            if (strippedTarget.StartsWith("mob_")) strippedTarget = strippedTarget[4..];
            if (strippedTarget.StartsWith("boss_")) strippedTarget = strippedTarget[5..];

            var bossTemplate = FindBossTemplate(targetBossId) ?? GameConstants.BossCatalog[0];


            double bossPower = bossTemplate.baseAttack * (1 + encounter.bossLevel * GameConstants.BossLevelScaleFactor)
                             + bossTemplate.baseDefense
                             + encounter.bossLevel;

            // 4. Battle Score = (Player Power − Boss Power) + Random Factor + Lucky Factor
            double randomFactor = (_random.NextDouble() * 2 - 1) * GameConstants.RandomFactorRange * playerPower;

            // Lucky Factor: 3 roll độc lập theo luckyRate
            double luckyFactor = 0;
            var luckyEffects = new List<string>();

            // 4a. Critical Hit: +0.3 × Attack(Total) vào điểm
            if (_random.NextDouble() <= effectiveStats.luckyRate)
            {
                luckyFactor += 0.3 * effectiveStats.attack;
                luckyEffects.Add("Critical Hit");
            }

            // 4b. Dodge: +0.1 × Boss Power vào điểm
            if (_random.NextDouble() <= effectiveStats.luckyRate)
            {
                luckyFactor += 0.1 * bossPower;
                luckyEffects.Add("Dodge");
            }

            // 4c. Damage Bonus: +0.1 × Player Power
            if (_random.NextDouble() <= effectiveStats.luckyRate)
            {
                luckyFactor += 0.1 * playerPower;
                luckyEffects.Add("Damage Bonus");
            }


            double battleScore = (playerPower - bossPower) + randomFactor + luckyFactor;
            bool isVictory = battleScore >= 0;

            _logger.LogInformation(
                "Battle: PP={PlayerPower:F1}, BP={BossPower:F1}, RF={RandomFactor:F1}, LF={LuckyFactor:F1}, Score={Score:F1}, Result={Result}",
                playerPower, bossPower, randomFactor, luckyFactor, battleScore, isVictory ? "Victory" : "Defeat");

            // 5. Cập nhật encounter
            encounter.playerHpAfter = isVictory ? effectiveStats.maxHp : 0;
            encounter.bossHpAfter = isVictory ? 0 : encounter.bossHpBefore;
            encounter.status = isVictory ? "Victory" : "Defeat";
            await _battleRepository.SaveEncounterAsync(encounter);

            // 6. Ghi Battle record vào DB (Mục 7 — trước đây không ghi)
            string battleId = Guid.NewGuid().ToString("N");
            var battleRecord = new Battle
            {
                battleId = battleId,
                encounterId = encounter.encounterId,
                playerPower = (int)Math.Round(playerPower),
                bossPower = (int)Math.Round(bossPower),
                battleType = "BossEncounter",
                status = "Completed",
                result = isVictory ? "Victory" : "Defeat",
                turnCount = 1,
                durationMs = 0,
                playerSnapshotJson = $"{{\"attack\":{effectiveStats.attack},\"defense\":{effectiveStats.defense},\"criticalRate\":{effectiveStats.criticalRate:F3},\"luckyRate\":{effectiveStats.luckyRate:F3}}}",
                bossSnapshotJson = $"{{\"bossId\":\"{bossTemplate.bossId}\",\"level\":{encounter.bossLevel},\"rarity\":\"{encounter.bossRarity}\"}}",
                rewardJson = "",
                battleTime = DateTime.UtcNow
            };
            await _battleRepository.SaveBattleAsync(battleRecord);

            // 7. Sinh chuỗi lượt đánh chi tiết (Multi-turn Battle Simulation) cho UI Playback
            int playerMaxHp = effectiveStats.maxHp > 0 ? effectiveStats.maxHp : 100;
            int bossMaxHp = encounter.bossHpBefore > 0 ? encounter.bossHpBefore : 100;

            var turns = GenerateBattleTurns(
                character.name,
                bossTemplate.name,
                playerMaxHp,
                bossMaxHp,
                playerPower,
                bossPower,
                isVictory,
                luckyEffects);

            // 8. Xử lý phần thưởng (Victory) hoặc Death (Defeat)
            BattleRewardData? rewards = null;

            if (isVictory)
            {
                // Record the defeated boss — chỉ lưu chapter boss, không lưu mob (mob_* prefix)
                bool isMob = encounter.bossId.StartsWith("mob_", StringComparison.OrdinalIgnoreCase);
                var isCatalogBoss = !isMob && GameShared.Config.GameConstants.BossCatalog.Any(b => b.bossId.Equals(encounter.bossId, StringComparison.OrdinalIgnoreCase));
                if (isCatalogBoss)
                {
                    try
                    {
                        var defeated = new DefeatedBoss
                        {
                            characterId = character.characterId,
                            bossId = encounter.bossId,
                            bossName = bossTemplate.name,
                            bossLevel = encounter.bossLevel,
                            encounterId = encounter.encounterId,
                            defeatedAt = DateTime.UtcNow
                        };
                        await _defeatedBossRepository.SaveDefeatedBossAsync(defeated);
                        _logger.LogInformation("Saved defeated boss {BossId} for character {CharacterId}", encounter.bossId, character.characterId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to save defeated boss {BossId} for character {CharacterId}", encounter.bossId, character.characterId);
                    }
                }

                // Mục 5: Loot System
                string effectiveRarity = !string.IsNullOrWhiteSpace(encounter.bossRarity)
                    ? encounter.bossRarity
                    : bossTemplate.rarity;

                int goldReward = GameConstants.CalculateGoldReward(encounter.bossLevel, effectiveRarity);
                int expReward = GameConstants.CalculateExpReward(encounter.bossLevel, effectiveRarity, character.level);
                character.gold += goldReward;
                await _characterService.ApplyExperienceAndLevelUp(character, expReward);

                var lootDTOs = await _inventoryService.GrantLootDropAsync(
                    character.characterId, effectiveRarity, battleId);

                // Khi người chơi hạ gục quái (mob_*) -> Tự động rớt Key Item của khu vực nếu chưa sở hữu
                string? keyItemToGrant = null;
                try
                {
                    var session = await _storyRepository.GetSessionByCharacterIdAsync(character.characterId);
                    string currentLoc = session?.currentLocation ?? character.currentLocationId ?? "ancient_cave";

                    if (currentLoc.Equals("ancient_cave", StringComparison.OrdinalIgnoreCase))
                    {
                        keyItemToGrant = "item_ancient_key";
                    }
                    else if (currentLoc.Equals("forgotten_temple", StringComparison.OrdinalIgnoreCase))
                    {
                        keyItemToGrant = "item_elemental_core";
                    }
                    else if (currentLoc.Equals("sunken_shipwreck", StringComparison.OrdinalIgnoreCase))
                    {
                        keyItemToGrant = "item_sea_compass";
                    }
                    else if (currentLoc.Equals("abyssal_trench", StringComparison.OrdinalIgnoreCase))
                    {
                        keyItemToGrant = "item_void_crystal";
                    }
                    else if (currentLoc.Equals("coral_palace", StringComparison.OrdinalIgnoreCase))
                    {
                        keyItemToGrant = "item_fire_core";
                    }
                    else if (currentLoc.Equals("sulfur_mines", StringComparison.OrdinalIgnoreCase))
                    {
                        keyItemToGrant = "item_obsidian_key";
                    }
                    else if (currentLoc.Equals("obsidian_peaks", StringComparison.OrdinalIgnoreCase))
                    {
                        keyItemToGrant = "item_dragon_blood_key";
                    }

                    if (!string.IsNullOrEmpty(keyItemToGrant))
                    {
                        var existingKeyItem = await _inventoryRepository.FindByCharacterAndItemAsync(character.characterId, keyItemToGrant);
                        if (existingKeyItem == null || existingKeyItem.quantity <= 0)
                        {
                            await _inventoryService.AddItemToInventoryAsync(character.characterId, keyItemToGrant, 1);
                            if (!lootDTOs.Any(l => l.itemId == keyItemToGrant))
                            {
                                lootDTOs.Add(new LootItemDTO { itemId = keyItemToGrant, quantity = 1 });
                            }
                            _logger.LogInformation("Tự động rớt Key Item '{KeyItemId}' cho nhân vật {CharacterId} sau khi hạ gục quái tại {Location}", keyItemToGrant, character.characterId, currentLoc);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not grant key item {KeyItem} to character {CharacterId}", keyItemToGrant, character.characterId);
                }

                rewards = new BattleRewardData
                {
                    goldEarned = goldReward,
                    expEarned = expReward,
                    lootItems = lootDTOs.Select(l => new LootItemData
                    {
                        itemId = l.itemId,
                        itemName = GameConstants.GetItemById(l.itemId)?.name ?? l.itemId,
                        quantity = l.quantity
                    }).ToList()
                };

                // Lưu character (gold đã cộng, XP đã xử lý trong ApplyExperienceAndLevelUp)
                await _characterRepository.SaveAsync(character);

                // Tự động chuyển Chương trong Story Session khi đánh bại Boss
                await AdvanceStoryChapterOnBossDefeatAsync(character.characterId, encounter.bossId);
            }
            else
            {
                // Mục 6: Death — HP=0, Status="Dead", ReviveTime
                character.hp = 0;
                character.status = "Dead";
                character.reviveTime = DateTime.UtcNow.AddMinutes(GameConstants.ReviveWaitMinutes);
                await _characterRepository.SaveAsync(character);

                _logger.LogInformation("Character {CharacterId} died. Revive at {ReviveTime}",
                    character.characterId, character.reviveTime);
            }

            // 9. Ghi nhận kết quả trận đấu vào mạch truyện (Story Session Actions)
            try
            {
                var session = await _storyRepository.GetSessionByCharacterIdAsync(character.characterId);
                if (session != null && session.status == "Active")
                {
                    // Nếu THẤT BẠI hoặc đang ở node "boss_room", reset currentNodeId về currentLocation chính để thoát khỏi phòng Boss
                    if (!isVictory || string.Equals(session.currentNodeId, "boss_room", StringComparison.OrdinalIgnoreCase))
                    {
                        session.currentNodeId = session.currentLocation;
                        session.updatedAt = DateTime.UtcNow;
                        await _storyRepository.SaveSessionAsync(session);
                        _logger.LogInformation("Reset session currentNodeId to '{Location}' for character {CharacterId} after battle outcome (isVictory={IsVictory})", session.currentLocation, character.characterId, isVictory);
                    }

                    var allRecentActions = await _storyRepository.GetActionsBySessionIdAsync(session.sessionId);
                    var turnNumber = allRecentActions.Count + 1;

                    var goldReward = rewards?.goldEarned ?? 0;
                    var expReward = rewards?.expEarned ?? 0;
                    var lootItemsDesc = (rewards?.lootItems != null && rewards.lootItems.Count > 0)
                        ? string.Join(", ", rewards.lootItems.Select(i => $"{i.itemName} (x{i.quantity})"))
                        : "Không có";

                    var outcomeText = isVictory ? "Chiến thắng" : "Thất bại";
                    var bossName = bossTemplate.name;

                    var narrativeText = $"[TRẬN ĐÁNH VỪA KẾT THÚC]\n" +
                                        $"Kết quả: {outcomeText}.\n" +
                                        $"Đối thủ: {bossName} (Cấp độ {encounter.bossLevel}).\n" +
                                        $"Phần thưởng: {goldReward} Vàng, {expReward} EXP.\n" +
                                        $"Vật phẩm nhận được: {lootItemsDesc}.";

                    var aiResponse = new StoryAiResponse
                    {
                        NarrativeText = narrativeText,
                        CurrentNodeId = session.currentNodeId,
                        CurrentLocation = session.currentLocation,
                        CurrentChapterId = session.currentChapterId,
                        StorySummary = session.storySummary,
                        ActionType = "battle_result",
                        TriggerBattle = false,
                        Choices = new List<StoryChoiceOption>
                        {
                            new StoryChoiceOption
                            {
                                label = "Tiếp tục",
                                description = "Sau cuộc chiến, bạn quay lại khu vực chính để tiếp tục cuộc hành trình.",
                                nextNodeId = session.currentNodeId
                            }
                        }
                    };

                    var action = new StoryAction
                    {
                        actionId = Guid.NewGuid().ToString("N"),
                        sessionId = session.sessionId,
                        playerInput = $"[TRẬN ĐÁNH] Đối mặt và quyết chiến với {bossName}",
                        aiResponse = narrativeText,
                        turnNumber = turnNumber,
                        actionType = "battle_result",
                        metadataJson = StoryAiResponseParser.Serialize(aiResponse),
                        createdAt = DateTime.UtcNow
                    };

                    await _storyRepository.SaveActionAsync(action);
                    _logger.LogInformation("Saved battle result StoryAction for session {SessionId}", session.sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save battle result StoryAction for character {CharacterId}", character.characterId);
            }

            return new BattleResolveResponse
            {
                battleId = battleId,
                encounterId = encounter.encounterId,
                isPlayerVictory = isVictory,
                playerPower = Math.Round(playerPower, 1),
                bossPower = Math.Round(bossPower, 1),
                battleScore = Math.Round(battleScore, 1),
                luckyEffects = luckyEffects,
                turns = turns,
                rewards = rewards,
                updatedCharacter = new GameShared.DTOs.Character.CharacterResponse
                {
                    characterId = character.characterId,
                    name = character.name,
                    level = character.level,
                    experience = character.experience,
                    hp = isVictory ? effectiveStats.maxHp : 0,
                    maxHp = effectiveStats.maxHp,
                    attack = effectiveStats.attack,
                    defense = effectiveStats.defense,
                    criticalRate = effectiveStats.criticalRate,
                    luckyRate = effectiveStats.luckyRate,
                    gold = character.gold,
                    status = character.status
                }
            };
        }

        // =====================================================================
        // PRIVATE HELPERS
        // =====================================================================

        private static Boss? FindBossTemplate(string? targetBossId)
        {
            if (string.IsNullOrWhiteSpace(targetBossId)) return null;

            string cleanTarget = targetBossId.Trim().ToLowerInvariant();
            bool isMobRequested = cleanTarget.StartsWith("mob_");
            bool isBossRequested = cleanTarget.StartsWith("boss_");

            string strippedTarget = cleanTarget;
            if (isMobRequested) strippedTarget = strippedTarget[4..];
            if (isBossRequested) strippedTarget = strippedTarget[5..];

            // 1. Exact match on bossId
            var exact = GameConstants.BossCatalog.FirstOrDefault(b =>
                b.bossId.Equals(cleanTarget, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            // 2. Exact match on name
            var nameMatch = GameConstants.BossCatalog.FirstOrDefault(b =>
                b.name.Equals(targetBossId, StringComparison.OrdinalIgnoreCase) ||
                b.name.Equals(strippedTarget.Replace("_", " "), StringComparison.OrdinalIgnoreCase));
            if (nameMatch != null) return nameMatch;

            // 3. Exact match with mob_ / boss_ prefix
            var prefixMatch = GameConstants.BossCatalog.FirstOrDefault(b =>
                b.bossId.Equals($"mob_{strippedTarget}", StringComparison.OrdinalIgnoreCase) ||
                b.bossId.Equals($"boss_{strippedTarget}", StringComparison.OrdinalIgnoreCase));
            if (prefixMatch != null) return prefixMatch;

            // 4. Substring match respecting request type (mob vs boss)
            if (isMobRequested)
            {
                var mobMatch = GameConstants.BossCatalog.FirstOrDefault(b =>
                    b.bossId.StartsWith("mob_", StringComparison.OrdinalIgnoreCase) &&
                    b.bossId.Contains(strippedTarget, StringComparison.OrdinalIgnoreCase));
                if (mobMatch != null) return mobMatch;
            }
            else if (isBossRequested)
            {
                var bossMatch = GameConstants.BossCatalog.FirstOrDefault(b =>
                    b.bossId.StartsWith("boss_", StringComparison.OrdinalIgnoreCase) &&
                    b.bossId.Contains(strippedTarget, StringComparison.OrdinalIgnoreCase));
                if (bossMatch != null) return bossMatch;
            }

            // 5. Fallback substring match
            return GameConstants.BossCatalog.FirstOrDefault(b =>
                b.bossId.Contains(strippedTarget, StringComparison.OrdinalIgnoreCase));
        }

        // Removed ScaleStat

        /// <summary>Build lookup dictionary từ equipped items cho CalculateEffectiveStats.</summary>
        private static Dictionary<string, Item> BuildItemLookup(IEnumerable<Inventory> equippedItems)
        {
            var lookup = new Dictionary<string, Item>();
            foreach (var inv in equippedItems)
            {
                if (!lookup.ContainsKey(inv.itemId))
                {
                    var item = GameConstants.GetItemById(inv.itemId);
                    if (item != null) lookup[inv.itemId] = item;
                }
            }
            return lookup;
        }

        /// <summary>
        /// Sinh danh sách lượt đánh chi tiết (Multi-turn battle simulation) cho UI Unity Playback.
        /// </summary>
        private List<BattleTurnData> GenerateBattleTurns(
            string playerName,
            string bossName,
            int playerMaxHp,
            int bossMaxHp,
            double playerPower,
            double bossPower,
            bool isVictory,
            List<string> luckyEffects)
        {
            var turns = new List<BattleTurnData>();
            int currentPlayerHp = playerMaxHp;
            int currentBossHp = bossMaxHp;

            int targetTurns = _random.Next(4, 7);

            if (isVictory)
            {
                // Người chơi CHIẾN THẮNG: Boss bị hạ gục về 0 HP ở lượt cuối
                int pDmgPerTurn = Math.Max(5, bossMaxHp / targetTurns);
                int bDmgPerTurn = Math.Max(1, (int)((playerMaxHp * 0.7) / targetTurns));

                for (int t = 1; t <= targetTurns; t++)
                {
                    bool isLastTurn = (t == targetTurns);
                    bool isCritThisTurn = luckyEffects.Contains("Critical Hit") && (isLastTurn || _random.NextDouble() < 0.3);

                    int pDmg = isLastTurn ? currentBossHp : Math.Min(currentBossHp - 1, isCritThisTurn ? (int)(pDmgPerTurn * 1.5) : pDmgPerTurn);
                    pDmg = Math.Max(1, pDmg);
                    currentBossHp = Math.Max(0, currentBossHp - pDmg);

                    string pLog = (currentBossHp <= 0)
                        ? (luckyEffects.Count > 0
                            ? $"🎉 {playerName} tung đòn dứt điểm hạ gục {bossName} ({string.Join(", ", luckyEffects)})!"
                            : $"🎉 {playerName} tung đòn dứt điểm hạ gục {bossName} CHIẾN THẮNG!")
                        : (isCritThisTurn
                            ? $"⚔️ {playerName} bộc phát đòn Chí Mạng vào {bossName} gây {pDmg} sát thương!"
                            : $"⚔️ {playerName} tấn công {bossName} gây {pDmg} sát thương!");

                    turns.Add(new BattleTurnData
                    {
                        attackerName = playerName,
                        logMessage = pLog,
                        damage = pDmg,
                        playerHpRemaining = currentPlayerHp,
                        bossHpRemaining = currentBossHp,
                        isCritical = isCritThisTurn
                    });

                    if (currentBossHp <= 0) break;

                    // Boss đánh trả
                    bool isDodge = luckyEffects.Contains("Dodge") && _random.NextDouble() < 0.5;
                    int bDmg = isDodge ? 0 : Math.Min(currentPlayerHp - 10, bDmgPerTurn);
                    bDmg = Math.Max(0, bDmg);
                    currentPlayerHp = Math.Max(1, currentPlayerHp - bDmg);

                    string bLog = isDodge
                        ? $"🛡️ {playerName} nhanh nhạy né tránh hoàn toàn đòn đánh của {bossName}!"
                        : $"👹 {bossName} phản công {playerName} gây {bDmg} sát thương!";

                    turns.Add(new BattleTurnData
                    {
                        attackerName = bossName,
                        logMessage = bLog,
                        damage = bDmg,
                        playerHpRemaining = currentPlayerHp,
                        bossHpRemaining = currentBossHp,
                        isCritical = false
                    });
                }
            }
            else
            {
                // Người chơi THẤT BẠI: Player bị đánh gục về 0 HP ở lượt cuối
                int pDmgPerTurn = Math.Max(1, (int)((bossMaxHp * 0.7) / targetTurns));
                int bDmgPerTurn = Math.Max(5, playerMaxHp / targetTurns);

                for (int t = 1; t <= targetTurns; t++)
                {
                    bool isLastTurn = (t == targetTurns);

                    // Player đánh trước
                    int pDmg = Math.Min(currentBossHp - 10, pDmgPerTurn);
                    pDmg = Math.Max(1, pDmg);
                    currentBossHp = Math.Max(1, currentBossHp - pDmg);

                    string pLog = $"⚔️ {playerName} tấn công {bossName} gây {pDmg} sát thương!";

                    turns.Add(new BattleTurnData
                    {
                        attackerName = playerName,
                        logMessage = pLog,
                        damage = pDmg,
                        playerHpRemaining = currentPlayerHp,
                        bossHpRemaining = currentBossHp,
                        isCritical = false
                    });

                    // Boss đánh trả
                    int bDmg = isLastTurn ? currentPlayerHp : Math.Min(currentPlayerHp - 1, bDmgPerTurn);
                    bDmg = Math.Max(1, bDmg);
                    currentPlayerHp = Math.Max(0, currentPlayerHp - bDmg);

                    string bLog = (currentPlayerHp <= 0)
                        ? $"💀 {bossName} tung đòn sấm sét đánh gục {playerName}! THẤT BẠI!"
                        : $"👹 {bossName} phản công {playerName} gây {bDmg} sát thương!";

                    turns.Add(new BattleTurnData
                    {
                        attackerName = bossName,
                        logMessage = bLog,
                        damage = bDmg,
                        playerHpRemaining = currentPlayerHp,
                        bossHpRemaining = currentBossHp,
                        isCritical = false
                    });

                    if (currentPlayerHp <= 0) break;
                }
            }

            return turns;
        }

        private async Task AdvanceStoryChapterOnBossDefeatAsync(string characterId, string bossId)
        {
            if (_storyRepository == null || string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(bossId)) return;

            try
            {
                var session = await _storyRepository.GetSessionByCharacterIdAsync(characterId);
                if (session == null || session.status != "Active") return;

                string normalizedBossId = bossId.Trim().ToLowerInvariant().Replace("boss_", "");

                string targetChapterId = "";
                string targetLocation = "";
                string chapterTitle = "";

                if (normalizedBossId == "goblin_king")
                {
                    targetChapterId = "chapter_2";
                    targetLocation = "sunken_shipwreck";
                    chapterTitle = "Chương 2: Vương Quốc Chìm Đắm (Xác Tàu Đắm)";
                }
                else if (normalizedBossId == "shadow_demon")
                {
                    targetChapterId = "chapter_3";
                    targetLocation = "sulfur_mines";
                    chapterTitle = "Chương 3: Vùng Đất Hoang Tàn Rực Lửa (Mỏ Lưu Huỳnh)";
                }
                else if (normalizedBossId == "dragon_king")
                {
                    targetChapterId = "chapter_4";
                    targetLocation = "start"; // Update with real chapter 4 location later
                    chapterTitle = "Chương 4: Đỉnh Núi Băng Giá";
                }

                if (!string.IsNullOrEmpty(targetChapterId))
                {
                    session.currentChapterId = targetChapterId;
                    session.currentLocation = targetLocation;
                    session.currentNodeId = targetLocation; // Reset node khỏi boss_room sau khi thắng Boss
                    session.updatedAt = DateTime.UtcNow;


                    string summaryNote = $" [ĐÃ HẠ GỤC BOSS {bossId.ToUpperInvariant()} - TIẾN SANG {chapterTitle.ToUpperInvariant()}]";
                    if (string.IsNullOrWhiteSpace(session.storySummary))
                    {
                        session.storySummary = summaryNote.Trim();
                    }
                    else if (!session.storySummary.Contains(targetChapterId, StringComparison.OrdinalIgnoreCase))
                    {
                        session.storySummary += summaryNote;
                    }

                    await _storyRepository.SaveSessionAsync(session);

                    var transitionAction = new StoryAction
                    {
                        actionId = Guid.NewGuid().ToString("N"),
                        sessionId = session.sessionId,
                        playerInput = $"[SỰ KIỆN CHIẾN THẮNG]: Đã tiêu diệt thành công Boss {bossId}!",
                        aiResponse = $"Vua Goblin ngã xuống! Mảnh Vỡ Lõi Nguyên Tố tỏa sáng rực rỡ giải trừ phong ấn. Bạn chính thức hoàn thành Chương 1 và bước sang {chapterTitle}!",
                        turnNumber = 999,
                        actionType = "chapter_transition",
                        metadataJson = $"{{\"currentChapterId\":\"{targetChapterId}\",\"currentLocation\":\"{targetLocation}\",\"defeatedBoss\":\"{bossId}\"}}",
                        createdAt = DateTime.UtcNow
                    };

                    await _storyRepository.SaveActionAsync(transitionAction);
                    _logger.LogInformation("Advanced session {SessionId} for character {CharacterId} to chapter {ChapterId} ({Location}) after defeating boss {BossId}",
                        session.sessionId, characterId, targetChapterId, targetLocation, bossId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-advance story chapter after boss defeat for character {CharacterId}", characterId);
            }
        }
    }
}

