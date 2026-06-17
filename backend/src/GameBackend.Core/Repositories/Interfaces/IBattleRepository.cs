using GameShared.Models;

namespace GameBackend.Core.Repositories.Interfaces
{
    public interface IBattleRepository
    {
        Task<BossEncounter?> GetEncounterByIdAsync(string encounterId);
        Task SaveEncounterAsync(BossEncounter encounter);
        Task<Battle?> GetBattleByIdAsync(string battleId);
        Task SaveBattleAsync(Battle battle);
        Task SaveLootDropAsync(LootDrop lootDrop);
    }
}
