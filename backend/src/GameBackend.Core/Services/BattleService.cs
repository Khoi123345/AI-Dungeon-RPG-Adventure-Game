using GameBackend.Core.Config;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameShared.DTOs.Battle;
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
            ILogger<BattleService> logger)
        {
            _bossRepository = bossRepository;
            _battleRepository = battleRepository;
            _characterRepository = characterRepository;
            _characterService = characterService;
            _inventoryService = inventoryService;
            _inventoryRepository = inventoryRepository;
            _defeatedBossRepository = defeatedBossRepository;
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

            Boss? template = null;
            if (!string.IsNullOrWhiteSpace(targetBossId))
            {
                template = GameConstants.BossCatalog.FirstOrDefault(b =>
                    targetBossId.Equals(b.bossId, StringComparison.OrdinalIgnoreCase) ||
                    targetBossId.StartsWith(b.bossId, StringComparison.OrdinalIgnoreCase) ||
                    b.bossId.StartsWith(targetBossId, StringComparison.OrdinalIgnoreCase));
            }

            string rarity = template != null ? template.rarity : GameConstants.RollBossRarity();
            if (template == null)
            {
                template = GameConstants.GetBossTemplateByRarity(rarity);
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
                bossHpBefore   = ScaleStat(template.baseHp, bossLevel),
                status         = "Active",
                encounterTime  = DateTime.UtcNow
            };

            if (existingEncounter == null)
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
                bossHp        = ScaleStat(template.baseHp, bossLevel),
                bossAttack    = ScaleStat(template.baseAttack, bossLevel),
                bossDefense   = ScaleStat(template.baseDefense, bossLevel),
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
            string baseBossId = encounter.bossId.Contains('_')
                ? string.Join("_", encounter.bossId.Split('_').SkipLast(1))
                : encounter.bossId;
            var bossTemplate = GameConstants.BossCatalog.FirstOrDefault(b => encounter.bossId.StartsWith(b.bossId))
                ?? GameConstants.BossCatalog[0];

            double bossPower = bossTemplate.baseAttack * (1 + encounter.bossLevel * GameConstants.BossLevelScaleFactor)
                             + bossTemplate.baseDefense
                             + encounter.bossLevel;

            // 4. Battle Score = (Player Power − Boss Power) + Random Factor + Lucky Factor
            double randomFactor = (_random.NextDouble() * 2 - 1) * GameConstants.RandomFactorRange * playerPower;

            // Lucky Factor: 3 roll độc lập theo luckyRate
            double luckyFactor = 0;
            var luckyEffects = new List<string>();

            // 4a. Critical Hit: +Attack(Total) vào điểm
            if (_random.NextDouble() <= effectiveStats.luckyRate)
            {
                luckyFactor += effectiveStats.attack;
                luckyEffects.Add("Critical Hit");
            }

            // 4b. Dodge: +0.2 × Boss Power vào điểm
            if (_random.NextDouble() <= effectiveStats.luckyRate)
            {
                luckyFactor += GameConstants.DodgeBonusRatio * bossPower;
                luckyEffects.Add("Dodge");
            }

            // 4c. Damage Bonus: +Random(0.1, 0.3) × Player Power
            if (_random.NextDouble() <= effectiveStats.luckyRate)
            {
                double bonusRatio = GameConstants.DamageBonusMin
                    + _random.NextDouble() * (GameConstants.DamageBonusMax - GameConstants.DamageBonusMin);
                luckyFactor += bonusRatio * playerPower;
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
                // Record the defeated boss if it's a valid boss catalog item
                var isCatalogBoss = GameShared.Config.GameConstants.BossCatalog.Any(b => b.bossId.Equals(encounter.bossId, StringComparison.OrdinalIgnoreCase));
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
                int goldReward = GameConstants.CalculateGoldReward(encounter.bossLevel, encounter.bossRarity);
                int expReward = GameConstants.CalculateExpReward(encounter.bossLevel, encounter.bossRarity, character.level);
                character.gold += goldReward;
                await _characterService.ApplyExperienceAndLevelUp(character, expReward);

                var lootDTOs = await _inventoryService.GrantLootDropAsync(
                    character.characterId, encounter.bossRarity, battleId);

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

        /// <summary>Scale stat theo level: baseStat × (1 + 0.08 × level)</summary>
        private static int ScaleStat(int baseStat, int level)
        {
            return Math.Max(1, (int)Math.Round(baseStat * (1.0 + 0.08 * level)));
        }

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
    }
}

