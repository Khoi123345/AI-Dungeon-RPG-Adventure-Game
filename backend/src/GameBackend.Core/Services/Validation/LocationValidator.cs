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

        private static readonly HashSet<string> CanonicalLocationNodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "ancient_cave", "forgotten_temple", "goblin_hideout",
            "sunken_shipwreck", "abyssal_trench", "coral_palace",
            "sulfur_mines", "obsidian_peaks", "dragon_nest"
        };

        public async Task ValidateAsync(GameRuleValidationContext context)
        {
            var response = context.Response;
            var rawLocation = response.CurrentLocation?.Trim();
            var requestedLocation = NormalizeLocationId(rawLocation);
            var currentLocation = context.Session.currentLocation ?? context.Character.currentLocationId ?? "ancient_cave";
            var normalizedCurrentLocation = NormalizeLocationId(currentLocation);
            var chapterId = (context.Session.currentChapterId ?? "1").ToLowerInvariant();

            HashSet<string> validLocations = chapterId switch
            {
                "2" or "chapter_2" => Chapter2Locations,
                "3" or "chapter_3" => Chapter3Locations,
                _ => Chapter1Locations
            };

            // 1. Lấy kho đồ nhân vật để kiểm tra Key Item
            var inventory = await _inventoryRepository.GetByCharacterIdAsync(context.Character.characterId);
            var itemIds = new HashSet<string>(
                inventory.Where(i => i != null && i.quantity > 0).Select(i => i.itemId),
                StringComparer.OrdinalIgnoreCase);

            if (IsSkippedLocationTransition(normalizedCurrentLocation, requestedLocation))
            {
                _logger.LogWarning("Blocked skipped location transition from {Current} directly to {Requested}.", normalizedCurrentLocation, requestedLocation);
                requestedLocation = normalizedCurrentLocation;
                response.NarrativeText = normalizedCurrentLocation switch
                {
                    "sulfur_mines" => "Rời Cung Điện San Hô, bạn đặt chân vào Mỏ Lưu Huỳnh — khu vực đầu tiên của Chương 3. Hơi nóng và khói độc phủ kín các đường hầm; bạn phải khám phá nơi này trước khi có thể tiến lên Đỉnh Núi Hắc Diệu Thạch.",
                    "sunken_shipwreck" => "Bạn bắt đầu Chương 2 tại Xác Tàu Đắm, nơi những thủy thủ chết đuối và sinh vật biển đang canh giữ con đường xuống vực sâu.",
                    "ancient_cave" => "Bạn tiếp tục khám phá Hang Động Cổ Xưa và chưa thể bỏ qua Đền Thờ Bị Lãng Quên để tiến thẳng tới Sào Huyệt Goblin.",
                    _ => response.NarrativeText
                };
            }

            // 2. Kiểm tra currentLocation: Nếu AI trả về địa điểm hợp lệ của Chương hiện tại
            if (!string.IsNullOrWhiteSpace(requestedLocation) && validLocations.Contains(requestedLocation))
            {
                // Kiểm tra ràng buộc chìa khóa (Key Items) để chống nhảy cóc Location
                if (requestedLocation != normalizedCurrentLocation && requestedLocation == "forgotten_temple" && !itemIds.Contains("item_ancient_key"))
                {
                    _logger.LogWarning("Blocked AI transition to 'forgotten_temple': player lacks item_ancient_key.");
                    requestedLocation = "ancient_cave";
                }
                else if (requestedLocation != normalizedCurrentLocation && requestedLocation == "goblin_hideout" && !itemIds.Contains("item_elemental_core"))
                {
                    _logger.LogWarning("Blocked AI transition to 'goblin_hideout': player lacks item_elemental_core.");
                    requestedLocation = itemIds.Contains("item_ancient_key") ? "forgotten_temple" : "ancient_cave";
                }
                else if (requestedLocation != normalizedCurrentLocation && requestedLocation == "abyssal_trench" && !itemIds.Contains("item_sea_compass"))
                {
                    _logger.LogWarning("Blocked AI transition to 'abyssal_trench': player lacks item_sea_compass.");
                    requestedLocation = "sunken_shipwreck";
                }
                else if (requestedLocation != normalizedCurrentLocation && requestedLocation == "coral_palace" && !itemIds.Contains("item_void_crystal"))
                {
                    _logger.LogWarning("Blocked AI transition to 'coral_palace': player lacks item_void_crystal.");
                    requestedLocation = itemIds.Contains("item_sea_compass") ? "abyssal_trench" : "sunken_shipwreck";
                }
                else if (requestedLocation != normalizedCurrentLocation && requestedLocation == "obsidian_peaks" && !itemIds.Contains("item_obsidian_key"))
                {
                    _logger.LogWarning("Blocked AI transition to 'obsidian_peaks': player lacks item_obsidian_key.");
                    requestedLocation = "sulfur_mines";
                }
                else if (requestedLocation != normalizedCurrentLocation && requestedLocation == "dragon_nest" && !itemIds.Contains("item_dragon_blood_key"))
                {
                    _logger.LogWarning("Blocked AI transition to 'dragon_nest': player lacks item_dragon_blood_key.");
                    requestedLocation = itemIds.Contains("item_obsidian_key") ? "obsidian_peaks" : "sulfur_mines";
                }

                context.Session.currentLocation = requestedLocation;
                response.CurrentLocation = requestedLocation;
            }
            else
            {
                // Nếu AI tự bịa địa điểm lạ, ép về vị trí chính thức của Session
                _logger.LogWarning("AI returned non-canonical location '{Requested}' for Chapter '{Chapter}'. Overriding to '{Current}'.", requestedLocation, chapterId, currentLocation);
                response.CurrentLocation = currentLocation;
                context.Session.currentLocation = currentLocation;
            }

            var requestedNode = NormalizeLocationId(response.CurrentNodeId);
            if (string.IsNullOrWhiteSpace(requestedNode))
            {
                _logger.LogInformation("currentNodeId is empty. Defaulting to '{Location}'", response.CurrentLocation);
                response.CurrentNodeId = response.CurrentLocation;
                context.Session.currentNodeId = response.CurrentLocation;
            }
            else
            {
                var effectiveLocation = NormalizeLocationId(response.CurrentLocation);
                var sanitizedNode = CanonicalLocationNodes.Contains(requestedNode) &&
                                    !requestedNode.Equals(effectiveLocation, StringComparison.OrdinalIgnoreCase)
                    ? effectiveLocation
                    : requestedNode;
                response.CurrentNodeId = sanitizedNode;
                context.Session.currentNodeId = sanitizedNode;
            }

            // 3. Lọc bỏ các choices nhảy cóc hoặc tiến khu vực khi thiếu Key Item
            SanitizeChoices(response, response.CurrentLocation, itemIds);
        }

        private void SanitizeChoices(StoryAiResponse response, string currentLocation, HashSet<string> itemIds)
        {
            if (response.Choices == null) return;
            var loc = NormalizeLocationId(currentLocation);

            response.Choices.RemoveAll(choice =>
            {
                if (choice == null) return false;
                var target = NormalizeLocationId(choice.nextNodeId);
                var labelLower = (choice.label ?? "").ToLowerInvariant();
                var descLower = (choice.description ?? "").ToLowerInvariant();
                var combinedText = $"{target} {labelLower} {descLower}";

                // Chống nhảy cóc vị trí 1 -> 3
                if (loc == "ancient_cave" && MentionsDestination(combinedText, "goblin_hideout")) return true;
                if (loc == "sunken_shipwreck" && (target == "coral_palace" || combinedText.Contains("coral_palace") || combinedText.Contains("cung điện san hô"))) return true;
                if (loc == "sulfur_mines" && (target == "dragon_nest" || combinedText.Contains("dragon_nest") || combinedText.Contains("tổ rồng"))) return true;

                // Chống tiến khu vực khi thiếu Key Item (kiểm tra CẢ ID, Label và Description)
                if (loc != "forgotten_temple" && !itemIds.Contains("item_ancient_key") && MentionsDestination(combinedText, "forgotten_temple")) return true;

                if (loc != "goblin_hideout" && !itemIds.Contains("item_elemental_core") && MentionsDestination(combinedText, "goblin_hideout")) return true;

                if (loc != "abyssal_trench" && !itemIds.Contains("item_sea_compass") && MentionsDestination(combinedText, "abyssal_trench")) return true;

                if (loc != "coral_palace" && !itemIds.Contains("item_void_crystal") && MentionsDestination(combinedText, "coral_palace")) return true;

                if (loc != "obsidian_peaks" && !itemIds.Contains("item_obsidian_key") && MentionsDestination(combinedText, "obsidian_peaks")) return true;

                if (loc != "dragon_nest" && !itemIds.Contains("item_dragon_blood_key") && MentionsDestination(combinedText, "dragon_nest")) return true;

                // Chống vào boss_room khi thiếu Key Item của khu vực 3
                if (target == "boss_room" || combinedText.Contains("ngai vàng") || combinedText.Contains("boss_room"))
                {
                    if (loc == "goblin_hideout" && !itemIds.Contains("item_elemental_core")) return true;
                    if (loc == "coral_palace" && !itemIds.Contains("item_void_crystal")) return true;
                    if (loc == "dragon_nest" && !itemIds.Contains("item_dragon_blood_key")) return true;
                }

                return false;
            });
        }

        private static string NormalizeLocationId(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var clean = raw.Trim().ToLowerInvariant();
            if (clean.StartsWith("location_")) clean = clean["location_".Length..];
            if (clean.StartsWith("loc_")) clean = clean["loc_".Length..];
            return clean;
        }

        private static bool MentionsDestination(string text, string destination)
        {
            var normalized = (text ?? string.Empty).ToLowerInvariant();
            return destination switch
            {
                "goblin_hideout" => normalized.Contains("goblin_hideout") ||
                                    normalized.Contains("sào huyệt goblin") ||
                                    normalized.Contains("sao huyet goblin") ||
                                    normalized.Contains("vua goblin"),
                "forgotten_temple" => normalized.Contains("forgotten_temple") || normalized.Contains("đền thờ bị lãng quên"),
                "abyssal_trench" => normalized.Contains("abyssal_trench") || normalized.Contains("rãnh sâu vô tận"),
                "coral_palace" => normalized.Contains("coral_palace") || normalized.Contains("cung điện san hô"),
                "obsidian_peaks" => normalized.Contains("obsidian_peaks") || normalized.Contains("đỉnh núi hắc diệu"),
                "dragon_nest" => normalized.Contains("dragon_nest") || normalized.Contains("tổ rồng"),
                _ => normalized.Contains(destination)
            };
        }

        private static bool IsSkippedLocationTransition(string currentLocation, string requestedLocation)
        {
            return (currentLocation, requestedLocation) switch
            {
                ("ancient_cave", "goblin_hideout") => true,
                ("sunken_shipwreck", "coral_palace") => true,
                ("sulfur_mines", "dragon_nest") => true,
                _ => false
            };
        }
    }
}

