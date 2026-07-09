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
        private readonly ILogger<BattleService> _logger;
        private readonly Random _random = new();

        public BattleService(
            IBossRepository bossRepository,
            IBattleRepository battleRepository,
            ICharacterRepository characterRepository,
            ICharacterService characterService,
            IInventoryService inventoryService,
            IInventoryRepository inventoryRepository,
            ILogger<BattleService> logger)
        {
            _bossRepository = bossRepository;
            _battleRepository = battleRepository;
            _characterRepository = characterRepository;
            _characterService = characterService;
            _inventoryService = inventoryService;
            _inventoryRepository = inventoryRepository;
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

            // Mục 3: Rarity roll (weighted random) — dùng GameConstants
            string rarity = GameConstants.RollBossRarity();
            var template = GameConstants.GetBossTemplateByRarity(rarity);

            // Mục 3: BossLevel = PlayerLevel + RarityModifier + Random(-3,3), clamp ≥ 1
            int bossLevel = GameConstants.CalculateBossLevel(character.level, rarity);

            var encounter = new BossEncounter
            {
                encounterId    = Guid.NewGuid().ToString("N"),
                characterId    = character.characterId,
                bossId         = $"{template.bossId}_{Guid.NewGuid().ToString("N")[..8]}",
                bossLevel      = bossLevel,
                bossRarity     = rarity,
                playerHpBefore = character.hp,
                bossHpBefore   = ScaleStat(template.baseHp, bossLevel),
                status         = "Active",
                encounterTime  = DateTime.UtcNow
            };

            await _battleRepository.SaveEncounterAsync(encounter);
            _logger.LogInformation("Boss spawned: {BossId} (Lv.{Level}, {Rarity}) for character: {CharacterId}",
                encounter.bossId, bossLevel, rarity, character.characterId);

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
            //    Player Power = Attack(Total, đã bao gồm equipment bonus) + Level Bonus
            var equippedItems = await _inventoryRepository.GetEquippedItemsAsync(character.characterId);
            var itemLookup = BuildItemLookup(equippedItems);
            var effectiveStats = _characterService.CalculateEffectiveStats(character, equippedItems, itemLookup);

            double playerPower = effectiveStats.attack + character.level * 2;

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
            encounter.playerHpAfter = isVictory ? character.hp : 0;
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

            // 7. Xử lý phần thưởng (Victory) hoặc Death (Defeat)
            BattleRewardData? rewards = null;
            var turns = new List<BattleTurnData>();

            if (isVictory)
            {
                // Mục 5: Loot System
                int goldReward = GameConstants.CalculateGoldReward(encounter.bossLevel, encounter.bossRarity);
                int expReward = GameConstants.CalculateExpReward(encounter.bossLevel, encounter.bossRarity);
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

                turns.Add(new BattleTurnData
                {
                    attackerName = character.name,
                    logMessage = luckyEffects.Count > 0
                        ? $"{character.name} chiến thắng với {string.Join(", ", luckyEffects)}! (Score: {battleScore:F1})"
                        : $"{character.name} chiến thắng! (Score: {battleScore:F1})",
                    damage = (int)Math.Round(playerPower),
                    playerHpRemaining = character.hp,
                    bossHpRemaining = 0,
                    isCritical = luckyEffects.Contains("Critical Hit")
                });

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

                turns.Add(new BattleTurnData
                {
                    attackerName = "Boss",
                    logMessage = $"{character.name} đã bị đánh bại. Hồi sinh sau {GameConstants.ReviveWaitMinutes} phút. (Score: {battleScore:F1})",
                    damage = (int)Math.Round(bossPower),
                    playerHpRemaining = 0,
                    bossHpRemaining = encounter.bossHpBefore,
                    isCritical = false
                });

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
                    hp = character.hp,
                    maxHp = character.maxHp,
                    attack = character.attack,
                    defense = character.defense,
                    criticalRate = character.criticalRate,
                    luckyRate = character.luckyRate,
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
    }
}
