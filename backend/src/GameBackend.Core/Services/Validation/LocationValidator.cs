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

            // 1. Lấy kho đồ nhân vật để kiểm tra Key Item
            var inventory = await _inventoryRepository.GetByCharacterIdAsync(context.Character.characterId);
            var itemIds = new HashSet<string>(inventory.Select(i => i.itemId), StringComparer.OrdinalIgnoreCase);

            // 2. Kiểm tra currentLocation: Nếu AI trả về địa điểm hợp lệ của Chương hiện tại
            if (!string.IsNullOrWhiteSpace(requestedLocation) && validLocations.Contains(requestedLocation))
            {
                // Kiểm tra ràng buộc chìa khóa (Key Items) để chống nhảy cóc Location
                if (requestedLocation == "forgotten_temple" && !itemIds.Contains("item_ancient_key"))
                {
                    _logger.LogWarning("Blocked AI transition to 'forgotten_temple': player lacks item_ancient_key.");
                    requestedLocation = "ancient_cave";
                }
                else if (requestedLocation == "goblin_hideout" && !itemIds.Contains("item_temple_key"))
                {
                    _logger.LogWarning("Blocked AI transition to 'goblin_hideout': player lacks item_temple_key.");
                    requestedLocation = itemIds.Contains("item_ancient_key") ? "forgotten_temple" : "ancient_cave";
                }
                else if (requestedLocation == "abyssal_trench" && !itemIds.Contains("item_sea_compass") && !itemIds.Contains("item_shipwreck_key"))
                {
                    _logger.LogWarning("Blocked AI transition to 'abyssal_trench': player lacks item_sea_compass.");
                    requestedLocation = "sunken_shipwreck";
                }
                else if (requestedLocation == "coral_palace" && !itemIds.Contains("item_void_crystal") && !itemIds.Contains("item_abyssal_key"))
                {
                    _logger.LogWarning("Blocked AI transition to 'coral_palace': player lacks item_void_crystal.");
                    requestedLocation = itemIds.Contains("item_sea_compass") ? "abyssal_trench" : "sunken_shipwreck";
                }
                else if (requestedLocation == "obsidian_peaks" && !itemIds.Contains("item_obsidian_key"))
                {
                    _logger.LogWarning("Blocked AI transition to 'obsidian_peaks': player lacks item_obsidian_key.");
                    requestedLocation = "sulfur_mines";
                }
                else if (requestedLocation == "dragon_nest" && !itemIds.Contains("item_dragon_blood_key"))
                {
                    _logger.LogWarning("Blocked AI transition to 'dragon_nest': player lacks item_dragon_blood_key.");
                    requestedLocation = itemIds.Contains("item_obsidian_key") ? "obsidian_peaks" : "sulfur_mines";
                }

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

            // 2. Chấp nhận mọi currentNodeId do AI tạo ra để mở rộng cốt truyện, chỉ cần không rỗng.
            var requestedNode = NormalizeLocationId(response.CurrentNodeId);
            if (string.IsNullOrWhiteSpace(requestedNode))
            {
                _logger.LogInformation("currentNodeId is empty. Defaulting to '{Location}'", response.CurrentLocation);
                response.CurrentNodeId = response.CurrentLocation;
                context.Session.currentNodeId = response.CurrentLocation;
            }
            else
            {
                response.CurrentNodeId = requestedNode;
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

