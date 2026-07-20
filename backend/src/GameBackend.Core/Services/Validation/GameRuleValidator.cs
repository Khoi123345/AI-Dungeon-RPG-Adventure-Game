using GameBackend.Core.Services.Interfaces;
using GameShared.DTOs.Story;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services.Validation
{
    public sealed class GameRuleValidator : IGameRuleValidator
    {
        private readonly IEnumerable<IGameRuleSubValidator> _validators;
        private readonly ILogger<GameRuleValidator> _logger;

        public GameRuleValidator(IEnumerable<IGameRuleSubValidator> validators, ILogger<GameRuleValidator> logger)
        {
            _validators = validators;
            _logger = logger;
        }

        public async Task<StoryAiResponse> ValidateAndSanitizeAsync(StorySession session, Character character, StoryAiResponse aiResponse)
        {
            var response = aiResponse ?? new StoryAiResponse();
            response.CharacterDelta ??= new StoryAiCharacterDelta();
            response.InventoryChanges ??= new List<StoryAiInventoryChange>();

            var context = new GameRuleValidationContext
            {
                Session = session,
                Character = character,
                Response = response
            };

            foreach (var validator in _validators)
            {
                await validator.ValidateAsync(context);
            }

            _logger.LogDebug("Game rule validation complete. TriggerBattle={TriggerBattle}, BossId={BossId}, InventoryChanges={InventoryCount}",
                context.Response.TriggerBattle,
                context.Response.BossId,
                context.Response.InventoryChanges.Count);

            return context.Response;
        }
    }
}
