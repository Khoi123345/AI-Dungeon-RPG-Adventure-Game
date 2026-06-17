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
                mp = 30,
                maxMp = 30,
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

        /// <summary>
        /// Logic level up tách từ GameProgressService.HandleLevelUpIfNeeded()
        /// </summary>
        public async Task<Character> ApplyExperienceAndLevelUp(Character character, int expGained)
        {
            character.experience += expGained;
            int requiredExp = character.level * 100;

            while (character.experience >= requiredExp)
            {
                character.experience -= requiredExp;
                character.level += 1;
                character.maxHp += 12;
                character.hp = character.maxHp;
                character.maxMp += 5;
                character.mp = character.maxMp;
                character.attack += 3;
                character.defense += 2;
                requiredExp = character.level * 100;

                _logger.LogInformation("Character {CharacterId} leveled up to {Level}", character.characterId, character.level);
            }

            await _characterRepository.SaveAsync(character);
            return character;
        }

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
                mp = c.mp,
                maxMp = c.maxMp,
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
