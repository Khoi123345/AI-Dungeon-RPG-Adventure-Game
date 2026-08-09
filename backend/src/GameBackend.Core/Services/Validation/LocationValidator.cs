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

        public async Task ValidateAsync(GameRuleValidationContext context)
        {
            var response = context.Response;
            var requestedLocation = response.CurrentLocation?.Trim();
            var currentLocation = context.Session.currentLocation ?? context.Character.currentLocationId ?? "ancient_cave";

            // 1. Kiểm tra xem vị trí do AI trả về có tồn tại trong hệ thống (Content Catalog) hay không
            if (!string.IsNullOrWhiteSpace(requestedLocation))
            {
                if (await _contentService.LocationExistsAsync(requestedLocation))
                {
                    // RÀNG BUỘC CỨNG KEY ITEM: Không cho phép nhảy vị trí nếu thiếu Key Item tương ứng
                    var inventory = await _inventoryRepository.GetByCharacterIdAsync(context.Character.characterId);
                    bool hasAncientKey = inventory != null && inventory.Any(i => string.Equals(i.itemId, "item_ancient_key", StringComparison.OrdinalIgnoreCase) && i.quantity > 0);
                    bool hasElementalCore = inventory != null && inventory.Any(i => string.Equals(i.itemId, "item_elemental_core", StringComparison.OrdinalIgnoreCase) && i.quantity > 0);
                    bool hasDragonBloodKey = inventory != null && inventory.Any(i => string.Equals(i.itemId, "item_dragon_blood_key", StringComparison.OrdinalIgnoreCase) && i.quantity > 0);

                    bool isAllowed = true;
                    if (string.Equals(requestedLocation, "forgotten_temple", StringComparison.OrdinalIgnoreCase) && !hasAncientKey)
                    {
                        isAllowed = false;
                        _logger.LogWarning("Chặn AI/Player chuyển sang 'forgotten_temple': Nhân vật {CharacterId} chưa có item_ancient_key trong kho đồ", context.Character.characterId);
                    }
                    else if (string.Equals(requestedLocation, "goblin_hideout", StringComparison.OrdinalIgnoreCase) && !hasElementalCore)
                    {
                        isAllowed = false;
                        _logger.LogWarning("Chặn AI/Player chuyển sang 'goblin_hideout': Nhân vật {CharacterId} chưa có item_elemental_core trong kho đồ", context.Character.characterId);
                    }
                    else if (string.Equals(requestedLocation, "dragon_nest", StringComparison.OrdinalIgnoreCase) && !hasDragonBloodKey)
                    {
                        isAllowed = false;
                        _logger.LogWarning("Chặn AI/Player chuyển sang 'dragon_nest': Nhân vật {CharacterId} chưa có item_dragon_blood_key trong kho đồ", context.Character.characterId);
                    }

                    if (isAllowed)
                    {
                        context.Session.currentLocation = requestedLocation;
                        if (string.IsNullOrWhiteSpace(response.CurrentNodeId))
                        {
                            response.CurrentNodeId = requestedLocation;
                        }
                    }
                    else
                    {
                        // Giữ nguyên vị trí hiện tại nếu chưa đủ điều kiện
                        response.CurrentLocation = currentLocation;
                        response.CurrentNodeId = context.Session.currentNodeId ?? currentLocation;
                    }
                }
                else
                {
                    _logger.LogInformation("Vị trí '{Location}' từ AI không tồn tại trong Content Catalog, giữ nguyên vị trí hiện tại của Session.", requestedLocation);
                    response.CurrentLocation = currentLocation;
                }
            }
            else
            {
                // Nếu AI không trả về location -> Giữ vị trí hiện tại
                response.CurrentLocation = currentLocation;
            }

            // Đồng bộ currentNodeId nếu AI để trống
            if (string.IsNullOrWhiteSpace(response.CurrentNodeId))
            {
                response.CurrentNodeId = context.Session.currentNodeId ?? response.CurrentLocation;
            }
        }
    }
}
