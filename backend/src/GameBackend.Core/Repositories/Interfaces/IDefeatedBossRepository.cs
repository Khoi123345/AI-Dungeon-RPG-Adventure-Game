using GameShared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameBackend.Core.Repositories.Interfaces
{
    public interface IDefeatedBossRepository
    {
        Task SaveDefeatedBossAsync(DefeatedBoss defeatedBoss);
        Task<List<DefeatedBoss>> GetDefeatedBossesByCharacterIdAsync(string characterId);
        Task<bool> HasDefeatedBossAsync(string characterId, string bossId);
        Task DeleteByCharacterIdAsync(string characterId);
    }
}
