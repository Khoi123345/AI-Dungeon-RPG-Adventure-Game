using GameBackend.Core.AIStory.Services;
using GameShared.DTOs.Story;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services.Validation
{
    public sealed class BossValidator : IGameRuleSubValidator
    {
        private readonly IContentService _contentService;
        private readonly ILogger<BossValidator> _logger;

        public BossValidator(IContentService contentService, ILogger<BossValidator> logger)
        {
            _contentService = contentService;
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

            if (!await _contentService.BossExistsAsync(response.BossId))
            {
                _logger.LogInformation("Rejected battle trigger because boss {BossId} does not exist in content", response.BossId);
                ResetBossFields(response);
                return;
            }

            var effectiveLocation = response.CurrentLocation ?? context.Session.currentLocation ?? context.Character.currentLocationId;
            if (!string.IsNullOrWhiteSpace(effectiveLocation) && !await _contentService.LocationExistsAsync(effectiveLocation))
            {
                _logger.LogInformation("Rejected battle trigger because boss {BossId} references invalid location {Location}", response.BossId, effectiveLocation);
                ResetBossFields(response);
                return;
            }

            response.BossName = string.IsNullOrWhiteSpace(response.BossName) ? response.BossId : response.BossName;
            var suggestedLevel = response.BossLevel ?? 1;
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
