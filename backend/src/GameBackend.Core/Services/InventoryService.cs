using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameShared.DTOs.Inventory;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(IInventoryRepository inventoryRepository, ILogger<InventoryService> logger)
        {
            _inventoryRepository = inventoryRepository;
            _logger = logger;
        }

        public async Task<InventoryResponse> GetInventoryAsync(string characterId)
        {
            var items = await _inventoryRepository.GetByCharacterIdAsync(characterId);

            return new InventoryResponse
            {
                characterId = characterId,
                totalSlots = 20,
                slots = items.Select(i => new InventorySlot
                {
                    inventoryId = i.inventoryId,
                    itemId = i.itemId,
                    quantity = i.quantity,
                    equipped = i.equipped,
                    slotIndex = i.slotIndex,
                    locked = i.locked
                }).ToList()
            };
        }

        public async Task AddItemToInventoryAsync(string characterId, string itemId, int quantity)
        {
            var existing = await _inventoryRepository.FindByCharacterAndItemAsync(characterId, itemId);
            if (existing != null)
            {
                existing.quantity += quantity;
                await _inventoryRepository.SaveAsync(existing);
                return;
            }

            var newSlot = new Inventory
            {
                inventoryId = Guid.NewGuid().ToString("N"),
                characterId = characterId,
                itemId = itemId,
                quantity = quantity,
                equipped = false,
                slotIndex = 0,
                locked = false,
                acquiredAt = DateTime.UtcNow
            };

            await _inventoryRepository.SaveAsync(newSlot);
            _logger.LogInformation("Item {ItemId} x{Quantity} added to character {CharacterId}", itemId, quantity, characterId);
        }
    }
}
