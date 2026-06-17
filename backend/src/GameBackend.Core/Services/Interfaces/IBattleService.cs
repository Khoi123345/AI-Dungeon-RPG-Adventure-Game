using GameShared.DTOs.Battle;

namespace GameBackend.Core.Services.Interfaces
{
    public interface IBattleService
    {
        Task<BossSpawnResponse> SpawnBossAsync(BossSpawnRequest request);
        Task<BattleResolveResponse> ResolveBattleAsync(BattleResolveRequest request);
    }
}
