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
        private readonly ILogger<CharacterService> _logger;

        public CharacterService(ICharacterRepository characterRepository, ILogger<CharacterService> logger)
        {
            _characterRepository = characterRepository;
            _logger = logger;
        }

        public async Task<CharacterResponse> GetCharacterAsync(string characterId)
        {
            var character = await _characterRepository.GetByIdAsync(characterId);
            if (character == null)
            {
                throw new Utils.GameNotFoundException("Character not found");
            }

            return MapToResponse(character);
        }

        public async Task<CharacterResponse> CreateCharacterAsync(CreateCharacterRequest request)
        {
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
                gold = 50,
                className = request.className ?? "Adventurer",
                status = "Alive",
                currentLocationId = "spawn_village",
                reviveTime = DateTime.UtcNow
            };

            await _characterRepository.SaveAsync(character);
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

            if (DateTime.UtcNow >= character.reviveTime)
            {
                // Đã qua thời gian chờ → tự động hồi sinh
                character.status = "Alive";
                character.hp = (int)(character.maxHp * GameConstants.RevivalHpRatio);
                character.hp = Math.Max(1, character.hp); // Tối thiểu 1 HP
                await _characterRepository.SaveAsync(character);

                _logger.LogInformation("Character {CharacterId} auto-revived with {Hp}/{MaxHp} HP",
                    character.characterId, character.hp, character.maxHp);
            }
            else
            {
                // Chưa tới giờ hồi sinh
                var remaining = character.reviveTime - DateTime.UtcNow;
                throw new Utils.GameValidationException(
                    $"Nhân vật đang chờ hồi sinh. Còn {remaining.Minutes} phút {remaining.Seconds} giây.");
            }
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

