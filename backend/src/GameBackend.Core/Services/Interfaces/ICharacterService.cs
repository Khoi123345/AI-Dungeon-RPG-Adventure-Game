using GameShared.DTOs.Character;
using GameShared.Models;

namespace GameBackend.Core.Services.Interfaces
{
    public interface ICharacterService
    {
        Task<CharacterResponse> GetCharacterAsync(string characterId);
        Task<CharacterResponse> CreateCharacterAsync(CreateCharacterRequest request);
        Task<Character> ApplyExperienceAndLevelUp(Character character, int expGained);
    }
}
