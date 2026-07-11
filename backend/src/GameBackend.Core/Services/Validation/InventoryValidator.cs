using GameBackend.Core.AIStory.Services;
using GameShared.DTOs.Story;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services.Validation
{
    public sealed class InventoryValidator : IGameRuleSubValidator
    {
        private const int MaxQuantityDelta = 20;

        private readonly IContentService _contentService;
        private readonly ILogger<InventoryValidator> _logger;

        public InventoryValidator(IContentService contentService, ILogger<InventoryValidator> logger)
        {
            _contentService = contentService;
            _logger = logger;
        }

        public async Task ValidateAsync(GameRuleValidationContext context)
        {
            var response = context.Response;
            if (response.InventoryChanges.Count == 0)
            {
                return;
            }

            var effectiveLocation = response.CurrentLocation ?? context.Session.currentLocation ?? context.Character.currentLocationId;
            var accepted = new List<StoryAiInventoryChange>();

            foreach (var change in response.InventoryChanges)
            {
                if (change == null || string.IsNullOrWhiteSpace(change.ItemId) || change.QuantityDelta == 0)
                {
                    continue;
                }

                if (!await _contentService.ItemExistsAsync(change.ItemId))
                {
                    _logger.LogInformation("Rejected inventory change for unknown item {ItemId}", change.ItemId);
                    continue;
                }

                var clampedQuantity = Math.Clamp(change.QuantityDelta, -MaxQuantityDelta, MaxQuantityDelta);
                if (clampedQuantity > 0 && !string.IsNullOrWhiteSpace(effectiveLocation) && !await _contentService.LocationExistsAsync(effectiveLocation))
                {
                    _logger.LogInformation("Rejected inventory reward item {ItemId} for invalid location {Location}", change.ItemId, effectiveLocation);
                    continue;
                }

                accepted.Add(new StoryAiInventoryChange
                {
                    ItemId = change.ItemId,
                    ItemName = string.IsNullOrWhiteSpace(change.ItemName) ? change.ItemId : change.ItemName,
                    QuantityDelta = clampedQuantity,
                    Equipped = change.Equipped,
                    SlotIndex = change.SlotIndex,
                    Locked = change.Locked
                });
            }

            response.InventoryChanges = accepted;
        }
    }
}
