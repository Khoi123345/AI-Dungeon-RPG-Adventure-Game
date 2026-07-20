using GameBackend.Core.AIStory.Services;
using GameShared.DTOs.Story;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services.Validation
{
    public sealed class LocationValidator : IGameRuleSubValidator
    {
        private readonly IContentService _contentService;
        private readonly ILogger<LocationValidator> _logger;

        public LocationValidator(IContentService contentService, ILogger<LocationValidator> logger)
        {
            _contentService = contentService;
            _logger = logger;
        }

        public async Task ValidateAsync(GameRuleValidationContext context)
        {
            var response = context.Response;
            var requestedLocation = response.CurrentLocation?.Trim();
            if (string.IsNullOrWhiteSpace(requestedLocation))
            {
                return;
            }

            if (!await _contentService.LocationExistsAsync(requestedLocation))
            {
                _logger.LogInformation("Invalid location '{Location}' returned by AI, falling back to current location.", requestedLocation);
                response.CurrentLocation = context.Session.currentLocation ?? context.Character.currentLocationId;
            }
        }
    }
}
