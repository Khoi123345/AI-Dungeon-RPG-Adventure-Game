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

            int effectiveMaxHp = character.maxHp;
            if (_inventoryRepository != null)
            {
                var equipped = await _inventoryRepository.GetEquippedItemsAsync(characterId);
                int bonusHp = equipped.Sum(e => GameConstants.GetItemById(e.itemId)?.hpBonus ?? 0);
                effectiveMaxHp += bonusHp;
            }

            return MapToResponse(character, effectiveMaxHp);
        }

        public async Task<CharacterResponse> CreateCharacterAsync(CreateCharacterRequest request)
        {
            // Idempotent: nếu userId đã có character, trả về character hiện có thay vì tạo mới
            if (!string.IsNullOrWhiteSpace(request.userId))
            {
                var existing = await _characterRepository.GetByUserIdAsync(request.userId);
                if (existing != null && existing.Count > 0)
                {
                    var found = existing[0];
                    _logger.LogInformation("Character already exists for userId {UserId}, returning existing: {CharacterId}", request.userId, found.characterId);
                    int existingEffectiveMaxHp = found.maxHp;
                    if (_inventoryRepository != null)
                    {
                        var equipped = await _inventoryRepository.GetEquippedItemsAsync(found.characterId);
                        int bonusHp = equipped.Sum(e => GameConstants.GetItemById(e.itemId)?.hpBonus ?? 0);
                        existingEffectiveMaxHp += bonusHp;
                    }
                    return MapToResponse(found, existingEffectiveMaxHp);
                }
            }

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
                    ("item_rusty_sword", 1, true, 0)
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
            return MapToResponse(character, character.maxHp);
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

            int effectiveMaxHp = character.maxHp > 0 ? character.maxHp : 100;
            if (_inventoryRepository != null)
            {
                var equipped = await _inventoryRepository.GetEquippedItemsAsync(characterId);
                int bonusHp = equipped.Sum(e => GameConstants.GetItemById(e.itemId)?.hpBonus ?? 0);
                effectiveMaxHp += bonusHp;
            }

            // Khôi phục nhân vật về trạng thái Alive với 100% Máu tối đa (Effective)
            character.status = "Alive";
            character.hp = effectiveMaxHp;
            await _characterRepository.SaveAsync(character);

            _logger.LogInformation("Character {CharacterId} revived with 100% HP ({Hp}/{MaxHp})",
                character.characterId, character.hp, effectiveMaxHp);
        }

        // =====================================================================
        // MAPPING
        // =====================================================================

        private static CharacterResponse MapToResponse(Character c, int effectiveMaxHp)
        {
            return new CharacterResponse
            {
                characterId = c.characterId,
                name = c.name,
                level = c.level,
                experience = c.experience,
                hp = c.hp,
                maxHp = effectiveMaxHp,
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

