using GameBackend.Core.Repositories.Interfaces;
using GameShared.DTOs.Story;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services.Validation
{
    public sealed class BossValidator : IGameRuleSubValidator
    {
        private readonly IBossRepository _bossRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ILogger<BossValidator> _logger;

        public BossValidator(IBossRepository bossRepository, ILocationRepository locationRepository, ILogger<BossValidator> logger)
        {
            _bossRepository = bossRepository;
            _locationRepository = locationRepository;
            _logger = logger;
        }

        public async Task ValidateAsync(GameRuleValidationContext context)
        {
            var response = context.Response;
            if (!response.TriggerBattle)
            {
                ResetBossFields(response);
                return;
            }

            if (string.IsNullOrWhiteSpace(response.BossId))
            {
                _logger.LogInformation("Rejected battle trigger because bossId is empty");
                ResetBossFields(response);
                return;
            }

            var boss = await _bossRepository.GetByIdAsync(response.BossId);
            if (boss == null)
            {
                _logger.LogInformation("Rejected battle trigger because boss {BossId} does not exist", response.BossId);
                ResetBossFields(response);
                return;
            }

            var effectiveLocation = response.CurrentLocation ?? context.Session.currentLocation ?? context.Character.currentLocationId;
            var canSpawn = await _locationRepository.CanSpawnBossAsync(boss.bossId, effectiveLocation ?? string.Empty);
            if (!canSpawn)
            {
                _logger.LogInformation("Rejected battle trigger because boss {BossId} cannot spawn at {Location}", boss.bossId, effectiveLocation);
                ResetBossFields(response);
                return;
            }

            response.BossId = boss.bossId;
            response.BossName = string.IsNullOrWhiteSpace(response.BossName) ? boss.name : response.BossName;
            var suggestedLevel = response.BossLevel ?? (boss.level > 0 ? boss.level : 1);
            response.BossLevel = Math.Clamp(suggestedLevel, 1, 200);
        }

        private static void ResetBossFields(StoryAiResponse response)
        {
            response.TriggerBattle = false;
            response.BossId = null;
            response.BossName = null;
            response.BossLevel = null;
        }
    }
}
