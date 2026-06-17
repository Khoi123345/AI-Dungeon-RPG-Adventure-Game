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
        private readonly ILogger<BattleService> _logger;
        private readonly Random _random = new();

        public BattleService(
            IBossRepository bossRepository,
            IBattleRepository battleRepository,
            ICharacterRepository characterRepository,
            ICharacterService characterService,
            IInventoryService inventoryService,
            ILogger<BattleService> logger)
        {
            _bossRepository = bossRepository;
            _battleRepository = battleRepository;
            _characterRepository = characterRepository;
            _characterService = characterService;
            _inventoryService = inventoryService;
            _logger = logger;
        }

        public async Task<BossSpawnResponse> SpawnBossAsync(BossSpawnRequest request)
        {
            var character = await _characterRepository.GetByIdAsync(request.characterId)
                ?? throw new Utils.GameNotFoundException("Character not found");

            if (character.status == "Dead")
            {
                throw new Utils.GameValidationException("Cannot spawn boss while character is dead");
            }

            string rarity = RollRarity();
            var template = GameConstants.GetBossTemplateByRarity(rarity);
            int bossLevel = CalculateBossLevel(character.level, rarity);

            var encounter = new BossEncounter
            {
                encounterId = Guid.NewGuid().ToString("N"),
                characterId = character.characterId,
                bossId = $"{template.bossId}_{Guid.NewGuid().ToString("N")[..8]}",
                bossLevel = bossLevel,
                playerHpBefore = character.hp,
                bossHpBefore = ScaleStat(template.baseHp, bossLevel),
                status = "Active",
                encounterTime = DateTime.UtcNow
            };

            await _battleRepository.SaveEncounterAsync(encounter);
            _logger.LogInformation("Boss spawned: {BossId} (Lv.{Level}) for character: {CharacterId}", encounter.bossId, bossLevel, character.characterId);

            return new BossSpawnResponse
            {
                encounterId = encounter.encounterId,
                bossId = encounter.bossId,
                bossName = template.name,
                bossRarity = rarity,
                bossLevel = bossLevel,
                bossHp = ScaleStat(template.baseHp, bossLevel),
                bossAttack = ScaleStat(template.baseAttack, bossLevel),
                bossDefense = ScaleStat(template.baseDefense, bossLevel),
                bossSpeed = template.speed,
                bossCriticalRate = template.criticalRate,
                bossImageUrl = template.imageUrl ?? ""
            };
        }

        public async Task<BattleResolveResponse> ResolveBattleAsync(BattleResolveRequest request)
        {
            var character = await _characterRepository.GetByIdAsync(request.characterId)
                ?? throw new Utils.GameNotFoundException("Character not found");

            var encounter = await _battleRepository.GetEncounterByIdAsync(request.encounterId)
                ?? throw new Utils.GameNotFoundException("Encounter not found");

            if (encounter.status != "Active")
            {
                throw new Utils.GameValidationException("Encounter is no longer active");
            }

            // Tính toán chiến đấu trên server (anti-cheat)
            int bossHp = encounter.bossHpBefore;
            int playerHp = character.hp;
            int playerDamage = Math.Max(1, character.attack + character.level * 2 - encounter.bossLevel);
            int bossDamage = Math.Max(1, encounter.bossLevel * 3 - character.defense / 2);
            var turns = new List<BattleTurnData>();
            bool isVictory = false;

            for (int turn = 0; turn < 10 && playerHp > 0 && bossHp > 0; turn++)
            {
                // Player attacks
                bool isCrit = _random.NextDouble() < character.criticalRate;
                int damage = isCrit ? (int)(playerDamage * 1.5) : playerDamage;
                bossHp = Math.Max(0, bossHp - damage);

                turns.Add(new BattleTurnData
                {
                    attackerName = character.name,
                    logMessage = isCrit
                        ? $"{character.name} tung đòn chí mạng gây {damage} sát thương!"
                        : $"{character.name} tấn công gây {damage} sát thương.",
                    damage = damage,
                    playerHpRemaining = playerHp,
                    bossHpRemaining = bossHp,
                    isCritical = isCrit
                });

                if (bossHp <= 0) { isVictory = true; break; }

                // Boss attacks
                bossHp = Math.Max(0, bossHp);
                playerHp = Math.Max(0, playerHp - bossDamage);

                turns.Add(new BattleTurnData
                {
                    attackerName = "Boss",
                    logMessage = $"Boss phản công gây {bossDamage} sát thương.",
                    damage = bossDamage,
                    playerHpRemaining = playerHp,
                    bossHpRemaining = bossHp,
                    isCritical = false
                });
            }

            // Cập nhật encounter
            encounter.playerHpAfter = playerHp;
            encounter.bossHpAfter = bossHp;
            encounter.status = isVictory ? "Victory" : "Defeat";
            await _battleRepository.SaveEncounterAsync(encounter);

            // Cập nhật character HP
            character.hp = Math.Clamp(playerHp, 0, character.maxHp);
            if (character.hp <= 0) character.status = "Dead";
            await _characterRepository.SaveAsync(character);

            // Xử lý phần thưởng
            BattleRewardData? rewards = null;
            if (isVictory)
            {
                int goldReward = encounter.bossLevel * 10;
                int expReward = encounter.bossLevel * 15;
                character.gold += goldReward;
                await _characterService.ApplyExperienceAndLevelUp(character, expReward);

                rewards = new BattleRewardData
                {
                    goldEarned = goldReward,
                    expEarned = expReward,
                    lootItems = new List<LootItemData>()
                };
            }

            string battleId = Guid.NewGuid().ToString("N");
            _logger.LogInformation("Battle resolved: {BattleId}, Result: {Result}", battleId, encounter.status);

            return new BattleResolveResponse
            {
                battleId = battleId,
                encounterId = encounter.encounterId,
                isPlayerVictory = isVictory,
                turns = turns,
                rewards = rewards,
                updatedCharacter = new GameShared.DTOs.Character.CharacterResponse
                {
                    characterId = character.characterId,
                    name = character.name,
                    level = character.level,
                    hp = character.hp,
                    maxHp = character.maxHp,
                    gold = character.gold,
                    status = character.status
                }
            };
        }

        private string RollRarity()
        {
            double roll = _random.NextDouble();
            if (roll <= 0.50) return "Common";
            if (roll <= 0.75) return "Uncommon";
            if (roll <= 0.90) return "Rare";
            if (roll <= 0.97) return "Epic";
            return "Legendary";
        }

        private int CalculateBossLevel(int playerLevel, string rarity)
        {
            var (minMod, maxMod) = rarity switch
            {
                "Common" => (0, 2),
                "Uncommon" => (1, 4),
                "Rare" => (2, 6),
                "Epic" => (3, 8),
                "Legendary" => (5, 10),
                _ => (0, 2)
            };
            return Math.Max(1, playerLevel + _random.Next(minMod, maxMod) + _random.Next(-1, 2));
        }

        private static int ScaleStat(int baseStat, int level)
        {
            return Math.Max(1, (int)Math.Round(baseStat * (1.0 + 0.08 * level)));
        }
    }
}
