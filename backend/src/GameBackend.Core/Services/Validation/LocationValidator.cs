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

            // 3. Fallback logic: Tự động chèn Lựa chọn chuyển địa điểm nếu người chơi đủ điều kiện (có Key Item) nhưng AI quên sinh lựa chọn
            EnsureTransitionChoices(response, itemIds);
        }

        private void EnsureTransitionChoices(StoryAiResponse response, HashSet<string> itemIds)
        {
            var currentLocation = response.CurrentLocation ?? string.Empty;

            // Chapter 1: ancient_cave -> forgotten_temple (cần item_ancient_key)
            if (currentLocation.Equals("ancient_cave", StringComparison.OrdinalIgnoreCase) && itemIds.Contains("item_ancient_key"))
            {
                if (!response.Choices.Any(c => (c.nextNodeId ?? "").Equals("forgotten_temple", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Injecting missing transition choice 'forgotten_temple' (player has item_ancient_key)");
                    response.Choices.Add(new StoryChoiceOption
                    {
                        label = "Tiến vào Đền Thờ Bị Lãng Quên",
                        description = "Sử dụng Chìa Khóa Cổ Xưa để mở cánh cửa đá khổng lồ dẫn vào đền thờ cổ.",
                        nextNodeId = "forgotten_temple"
                    });
                }
            }

            // Chapter 1: forgotten_temple -> goblin_hideout (cần item_elemental_core)
            if (currentLocation.Equals("forgotten_temple", StringComparison.OrdinalIgnoreCase) && itemIds.Contains("item_elemental_core"))
            {
                if (!response.Choices.Any(c => (c.nextNodeId ?? "").Equals("goblin_hideout", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Injecting missing transition choice 'goblin_hideout' (player has item_elemental_core)");
                    response.Choices.Add(new StoryChoiceOption
                    {
                        label = "Đột kích Sào Huyệt Goblin",
                        description = "Lõi Nguyên Tố tỏa sáng, chỉ đường dẫn sâu vào sào huyệt của vua Goblin.",
                        nextNodeId = "goblin_hideout"
                    });
                }
            }

            // Chapter 2: sunken_shipwreck -> abyssal_trench (cần item_sea_compass)
            if (currentLocation.Equals("sunken_shipwreck", StringComparison.OrdinalIgnoreCase) && itemIds.Contains("item_sea_compass"))
            {
                if (!response.Choices.Any(c => (c.nextNodeId ?? "").Equals("abyssal_trench", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Injecting missing transition choice 'abyssal_trench' (player has item_sea_compass)");
                    response.Choices.Add(new StoryChoiceOption
                    {
                        label = "Lặn xuống Rãnh Sâu Vô Tận",
                        description = "Sử dụng Hải Đồ để tìm lối xuống vùng vực sâu tăm tối dưới đáy đại dương.",
                        nextNodeId = "abyssal_trench"
                    });
                }
            }

            // Chapter 2: abyssal_trench -> coral_palace (cần item_void_crystal)
            if (currentLocation.Equals("abyssal_trench", StringComparison.OrdinalIgnoreCase) && itemIds.Contains("item_void_crystal"))
            {
                if (!response.Choices.Any(c => (c.nextNodeId ?? "").Equals("coral_palace", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Injecting missing transition choice 'coral_palace' (player has item_void_crystal)");
                    response.Choices.Add(new StoryChoiceOption
                    {
                        label = "Tiến vào Cung Điện San Hô",
                        description = "Pha Lê Hư Không cộng hưởng, giải trừ phong ấn dẫn lối vào cung điện san hô cổ xưa.",
                        nextNodeId = "coral_palace"
                    });
                }
            }

            // Chapter 3: sulfur_mines -> obsidian_peaks (cần item_obsidian_key)
            if (currentLocation.Equals("sulfur_mines", StringComparison.OrdinalIgnoreCase) && itemIds.Contains("item_obsidian_key"))
            {
                if (!response.Choices.Any(c => (c.nextNodeId ?? "").Equals("obsidian_peaks", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Injecting missing transition choice 'obsidian_peaks' (player has item_obsidian_key)");
                    response.Choices.Add(new StoryChoiceOption
                    {
                        label = "Trèo lên Đỉnh Núi Hắc Diệu Thạch",
                        description = "Chìa Khóa Hắc Diệu Thạch rung chuyển, kích hoạt thang nâng đá cổ xưa đưa bạn lên đỉnh núi.",
                        nextNodeId = "obsidian_peaks"
                    });
                }
            }

            // Chapter 3: obsidian_peaks -> dragon_nest (cần item_dragon_blood_key)
            if (currentLocation.Equals("obsidian_peaks", StringComparison.OrdinalIgnoreCase) && itemIds.Contains("item_dragon_blood_key"))
            {
                if (!response.Choices.Any(c => (c.nextNodeId ?? "").Equals("dragon_nest", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Injecting missing transition choice 'dragon_nest' (player has item_dragon_blood_key)");
                    response.Choices.Add(new StoryChoiceOption
                    {
                        label = "Tiến vào Tổ Rồng",
                        description = "Chìa Khóa Long Huyết hấp thụ nhiệt lượng nham thạch, mở lối vào hang ổ của vua Rồng.",
                        nextNodeId = "dragon_nest"
                    });
                }
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

