using GameShared.DTOs.Character;
using GameShared.Models;

namespace GameBackend.Core.Services.Interfaces
{
    public interface ICharacterService
    {
        Task<CharacterResponse> GetCharacterAsync(string characterId);
        Task<CharacterResponse> CreateCharacterAsync(CreateCharacterRequest request);
        Task<Character> ApplyExperienceAndLevelUp(Character character, int expGained);

        /// <summary>
        /// Tính chỉ số thực tế: Stat(gốc) + Σ(bonus từ equipped items).
        /// Áp dụng cho MaxHp, Attack, Defense, CriticalRate. Hàm thuần (không async).
        /// </summary>
        CharacterStats CalculateEffectiveStats(
            Character character,
            IEnumerable<Inventory> equippedItems,
            IDictionary<string, Item> itemLookup);

        /// <summary>
        /// Kiểm tra nhân vật còn sống không. Nếu Dead và đã qua ReviveTime → tự động hồi sinh.
        /// Nếu Dead và chưa tới ReviveTime → throw exception.
        /// </summary>
        Task EnsureAliveOrAutoReviveAsync(string characterId);
    }
}
