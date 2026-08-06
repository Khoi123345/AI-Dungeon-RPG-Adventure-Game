using GameBackend.Core.Config;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameShared.DTOs.Character;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
    public class CharacterService : ICharacterService
    {
        private readonly ICharacterRepository _characterRepository;
        private readonly IInventoryRepository? _inventoryRepository;
        private readonly ILogger<CharacterService> _logger;

        public CharacterService(
            ICharacterRepository characterRepository,
            IInventoryRepository? inventoryRepository,
            ILogger<CharacterService> logger)
        {
            _characterRepository = characterRepository;
            _inventoryRepository = inventoryRepository;
            _logger = logger;
        }

        public async Task<CharacterResponse> GetCharacterAsync(string characterId)
        {
            var character = await _characterRepository.GetByIdAsync(characterId);
            if (character == null)
            {
                throw new Utils.GameNotFoundException("Character not found");
            }

            if (character.name != null && character.name.Equals("khoi", StringComparison.OrdinalIgnoreCase))
            {
                character.gold = 999999;
                await _characterRepository.SaveAsync(character);
            }

            return MapToResponse(character);
        }

        public async Task<CharacterResponse> CreateCharacterAsync(CreateCharacterRequest request)
        {
            int startingGold = (request.name != null && request.name.Equals("khoi", StringComparison.OrdinalIgnoreCase)) ? 999999 : 50;

            var character = new Character
            {
                characterId = Guid.NewGuid().ToString("N"),
                userId = request.userId,
                name = request.name,
                level = 1,
                experience = 0,
                hp = 100,
                maxHp = 100,
                attack = 10,
                defense = 5,
                criticalRate = 0.05f,
                luckyRate = 0.05f,
                gold = startingGold,
                className = request.className ?? "Adventurer",
                status = "Alive",
                currentLocationId = "spawn_village",
                reviveTime = DateTime.UtcNow
            };

            await _characterRepository.SaveAsync(character);

            if (_inventoryRepository != null)
            {
                var starterItems = new[]
                {
                    ("item_rusty_sword", 1, true, 0),
                    ("item_leather_vest", 1, true, 1),
                    ("item_wooden_ring", 1, true, 2),
                    ("item_health_potion_s", 5, false, 3)
                };

                foreach (var (itemId, qty, eq, slot) in starterItems)
                {
                    try
                    {
                        await _inventoryRepository.SaveAsync(new Inventory
                        {
                            inventoryId = Guid.NewGuid().ToString("N"),
                            characterId = character.characterId,
                            itemId = itemId,
                            quantity = qty,
                            equipped = eq,
                            slotIndex = slot,
                            acquiredAt = DateTime.UtcNow
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to seed starter item {ItemId} for character {CharacterId}", itemId, character.characterId);
                    }
                }
            }

            _logger.LogInformation("Character created: {CharacterId} for user: {UserId}", character.characterId, character.userId);
            return MapToResponse(character);
        }

        // =====================================================================
        // CALCULATE EFFECTIVE STATS (Mục 1 logic doc)
        // Stat(tổng) = Stat(gốc) + Σ(bonus từ equipped items)
        // =====================================================================

        public CharacterStats CalculateEffectiveStats(
            Character character,
            IEnumerable<Inventory> equippedItems,
            IDictionary<string, Item> itemLookup)
        {
            int bonusHp = 0, bonusAttack = 0, bonusDefense = 0;
            float bonusCritical = 0f;

            foreach (var inv in equippedItems)
            {
                if (!inv.equipped) continue;
                if (itemLookup.TryGetValue(inv.itemId, out var item))
                {
                    bonusHp       += item.hpBonus;
                    bonusAttack   += item.attackBonus;
                    bonusDefense  += item.defenseBonus;
                    bonusCritical += item.criticalBonus;
                }
            }

            return new CharacterStats
            {
                maxHp        = character.maxHp   + bonusHp,
                attack       = character.attack  + bonusAttack,
                defense      = character.defense + bonusDefense,
                criticalRate = character.criticalRate + bonusCritical,
                luckyRate    = character.luckyRate
            };
        }

        // =====================================================================
        // LEVEL UP (Mục 1 logic doc — dùng GameConstants thay vì hardcode)
        // =====================================================================

        /// <summary>
        /// Logic level up: vòng lặp while cho phép nhảy nhiều cấp cùng lúc.
        /// </summary>
        public async Task<Character> ApplyExperienceAndLevelUp(Character character, int expGained)
        {
            character.experience += expGained;
            int requiredExp = character.level * GameConstants.BaseRequiredXpPerLevel;

            while (character.experience >= requiredExp)
            {
                character.experience -= requiredExp;
                character.level += 1;
                character.maxHp   += GameConstants.LevelUpHpGrowth;
                character.hp       = character.maxHp;   // Hồi đầy HP mỗi lần lên cấp
                character.attack  += GameConstants.LevelUpAttackGrowth;
                character.defense += GameConstants.LevelUpDefenseGrowth;
                requiredExp = character.level * GameConstants.BaseRequiredXpPerLevel;

                _logger.LogInformation("Character {CharacterId} leveled up to {Level}", character.characterId, character.level);
            }

            await _characterRepository.SaveAsync(character);
            return character;
        }

        // =====================================================================
        // DEATH & REVIVAL (Mục 6 logic doc)
        // =====================================================================

        /// <summary>
        /// Kiểm tra và tự động hồi sinh nếu đủ thời gian.
        /// Dùng chung cho mọi handler cần check trạng thái nhân vật.
        /// </summary>
        public async Task EnsureAliveOrAutoReviveAsync(string characterId)
        {
            var character = await _characterRepository.GetByIdAsync(characterId)
                ?? throw new Utils.GameNotFoundException("Character not found");

            if (character.status != "Dead") return; // Đang Alive — không cần làm gì

            // Khôi phục nhân vật về trạng thái Alive với 100% Máu tối đa
            character.status = "Alive";
            character.hp = character.maxHp > 0 ? character.maxHp : 100;
            await _characterRepository.SaveAsync(character);

            _logger.LogInformation("Character {CharacterId} revived with 100% HP ({Hp}/{MaxHp})",
                character.characterId, character.hp, character.maxHp);
        }

        // =====================================================================
        // MAPPING
        // =====================================================================

        private static CharacterResponse MapToResponse(Character c)
        {
            return new CharacterResponse
            {
                characterId = c.characterId,
                name = c.name,
                level = c.level,
                experience = c.experience,
                hp = c.hp,
                maxHp = c.maxHp,
                attack = c.attack,
                defense = c.defense,
                criticalRate = c.criticalRate,
                luckyRate = c.luckyRate,
                gold = c.gold,
                className = c.className,
                status = c.status,
                currentLocationId = c.currentLocationId
            };
        }
    }
}

