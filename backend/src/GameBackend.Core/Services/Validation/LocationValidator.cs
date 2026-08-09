using System;
using System.Linq;
using System.Threading.Tasks;
using GameBackend.Core.AIStory.Services;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.DTOs.Story;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services.Validation
{
    public sealed class LocationValidator : IGameRuleSubValidator
    {
        private readonly IContentService _contentService;
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ILogger<LocationValidator> _logger;

        public LocationValidator(
            IContentService contentService,
            IInventoryRepository inventoryRepository,
            ILogger<LocationValidator> logger)
        {
            _contentService = contentService;
            _inventoryRepository = inventoryRepository;
            _logger = logger;
        }

        private static readonly HashSet<string> Chapter1Locations = new(StringComparer.OrdinalIgnoreCase)
        {
            "ancient_cave", "forgotten_temple", "goblin_hideout", "boss_room"
        };

        private static readonly HashSet<string> Chapter2Locations = new(StringComparer.OrdinalIgnoreCase)
        {
            "sunken_shipwreck", "abyssal_trench", "coral_palace", "boss_room"
        };

        private static readonly HashSet<string> Chapter3Locations = new(StringComparer.OrdinalIgnoreCase)
        {
            "sulfur_mines", "obsidian_peaks", "dragon_nest", "boss_room"
        };

        public async Task ValidateAsync(GameRuleValidationContext context)
        {
            var response = context.Response;
            var rawLocation = response.CurrentLocation?.Trim();
            var requestedLocation = NormalizeLocationId(rawLocation);
            var currentLocation = context.Session.currentLocation ?? context.Character.currentLocationId ?? "ancient_cave";
            var chapterId = (context.Session.currentChapterId ?? "1").ToLowerInvariant();

            HashSet<string> validLocations = chapterId switch
            {
                "2" or "chapter_2" => Chapter2Locations,
                "3" or "chapter_3" => Chapter3Locations,
                _ => Chapter1Locations
            };

            // 1. Kiểm tra currentLocation: Nếu AI trả về địa điểm hợp lệ của Chương hiện tại
            if (!string.IsNullOrWhiteSpace(requestedLocation) && validLocations.Contains(requestedLocation))
            {
                context.Session.currentLocation = requestedLocation;
                response.CurrentLocation = requestedLocation;
            }
            else
            {
                // Nếu AI tự bịa địa điểm lạ (như coastline_exploration, beach), ép về vị trí chính thức của Session
                _logger.LogWarning("AI returned non-canonical location '{Requested}' for Chapter '{Chapter}'. Overriding to '{Current}'.", requestedLocation, chapterId, currentLocation);
                response.CurrentLocation = currentLocation;
                context.Session.currentLocation = currentLocation;
            }

            // 2. Kiểm tra currentNodeId: Lọc bỏ hoàn toàn các node tự bịa lạ không thuộc danh sách chính thức
            var requestedNode = NormalizeLocationId(response.CurrentNodeId);
            if (string.IsNullOrWhiteSpace(requestedNode) || (!validLocations.Contains(requestedNode) && !await _contentService.LocationExistsAsync(requestedNode)))
            {
                _logger.LogInformation("Sanitizing non-canonical currentNodeId '{Node}' to '{Location}'", response.CurrentNodeId, response.CurrentLocation);
                response.CurrentNodeId = response.CurrentLocation;
                context.Session.currentNodeId = response.CurrentLocation;
            }
            else
            {
                context.Session.currentNodeId = requestedNode;
            }
        }


        private static string NormalizeLocationId(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var clean = raw.Trim().ToLowerInvariant();
            if (clean.StartsWith("location_")) clean = clean["location_".Length..];
            if (clean.StartsWith("loc_")) clean = clean["loc_".Length..];
            return clean;
        }
    }
}

