using GameBackend.Core.Config;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameShared.DTOs.Inventory;
using GameShared.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameBackend.Core.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ICharacterRepository _characterRepository;
        private readonly IBattleRepository _battleRepository;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(
            IInventoryRepository inventoryRepository,
            ICharacterRepository characterRepository,
            IBattleRepository battleRepository,
            ILogger<InventoryService> logger)
        {
            _inventoryRepository = inventoryRepository;
            _characterRepository = characterRepository;
            _battleRepository = battleRepository;
            _logger = logger;
        }

        // =====================================================================
        // GET INVENTORY
        // =====================================================================

        public async Task<InventoryResponse> GetInventoryAsync(string characterId)
        {
            var items = await _inventoryRepository.GetByCharacterIdAsync(characterId);

            return new InventoryResponse
            {
                characterId = characterId,
                totalSlots = GameConstants.MaxInventorySlots,
                slots = items.Select(i => BuildInventorySlot(i)).ToList()
            };
        }

        // =====================================================================
        // GET ITEM DETAIL
        // =====================================================================

        public Task<ItemDetailResponse?> GetItemDetailAsync(string itemId)
        {
            var item = GameConstants.GetItemById(itemId);
            if (item == null) return Task.FromResult<ItemDetailResponse?>(null);

            return Task.FromResult<ItemDetailResponse?>(new ItemDetailResponse
            {
                itemId        = item.itemId,
                name          = item.name,
                rarity        = item.rarity,
                itemType      = item.itemType,
                slotType      = item.slotType,
                attackBonus   = item.attackBonus,
                defenseBonus  = item.defenseBonus,
                hpBonus       = item.hpBonus,
                criticalBonus = item.criticalBonus,
                description   = item.description,
                imageUrl      = item.imageUrl,
                stackable     = item.stackable,
                sellPrice     = item.sellPrice,
                buyPrice      = item.buyPrice,
                requiredLevel = item.requiredLevel,
                effectJson    = item.effectJson
            });
        }

        // =====================================================================
        // ADD ITEM TO INVENTORY (Mục 2.3 logic doc)
        // =====================================================================

        public async Task AddItemToInventoryAsync(string characterId, string itemId, int quantity)
        {
            var existing = await _inventoryRepository.FindByCharacterAndItemAsync(characterId, itemId);

            if (existing != null)
            {
                // Item đã có trong kho — cộng dồn số lượng (không tốn thêm ô)
                existing.quantity += quantity;
                await _inventoryRepository.SaveAsync(existing);
                return;
            }

            // Item chưa có — kiểm tra capacity trước khi tạo bản ghi mới (Mục 2.5)
            int currentSlots = await _inventoryRepository.CountSlotsAsync(characterId);
            if (currentSlots >= GameConstants.MaxInventorySlots)
                throw new GameValidationException($"Kho đồ đã đầy ({GameConstants.MaxInventorySlots} ô).");

            var newSlot = new Inventory
            {
                inventoryId = Guid.NewGuid().ToString("N"),
                characterId = characterId,
                itemId      = itemId,
                quantity    = quantity,
                equipped    = false,
                slotIndex   = 0,
                locked      = false,
                acquiredAt  = DateTime.UtcNow
            };

            await _inventoryRepository.SaveAsync(newSlot);
            _logger.LogInformation("Item {ItemId} x{Quantity} added to character {CharacterId}", itemId, quantity, characterId);
        }

        // =====================================================================
        // EQUIP ITEM (Mục 2.1 logic doc)
        // =====================================================================

        public async Task<InventoryResponse> EquipItemAsync(string characterId, string inventoryId)
        {
            // 1. Lấy inventory record và kiểm tra ownership
            var invRecord = await _inventoryRepository.GetByInventoryIdAsync(inventoryId)
                ?? throw new GameNotFoundException("Không tìm thấy vật phẩm trong kho đồ.");

            if (invRecord.characterId != characterId)
                throw new GameValidationException("Vật phẩm không thuộc nhân vật này.");

            // 2. Tra cứu thông tin item từ catalog
            var item = GameConstants.GetItemById(invRecord.itemId)
                ?? throw new GameNotFoundException($"Không tìm thấy thông tin item '{invRecord.itemId}' trong catalog.");

            if (item.itemType == "Consumable")
                throw new GameValidationException("Không thể trang bị vật phẩm tiêu hao.");

            // 3. Kiểm tra requiredLevel
            var character = await _characterRepository.GetByIdAsync(characterId)
                ?? throw new GameNotFoundException("Không tìm thấy nhân vật.");

            if (character.level < item.requiredLevel)
                throw new GameValidationException(
                    $"Nhân vật cần đạt cấp {item.requiredLevel} để trang bị '{item.name}'. (Cấp hiện tại: {character.level})");

            // 4. Tìm và gỡ item cùng item_type đang equipped (mục 2.1 bước 3-4)
            var equippedItems = await _inventoryRepository.GetEquippedItemsAsync(characterId);
            foreach (var eq in equippedItems)
            {
                var eqItem = GameConstants.GetItemById(eq.itemId);
                if (eqItem?.itemType == item.itemType && eq.inventoryId != inventoryId)
                {
                    eq.equipped = false;
                    await _inventoryRepository.SaveAsync(eq);
                    _logger.LogInformation("Unequipped old item {ItemId} (type={ItemType}) for character {CharId}", eq.itemId, item.itemType, characterId);
                }
            }

            // 5. Trang bị item mới (mục 2.1 bước 5)
            invRecord.equipped = true;
            await _inventoryRepository.SaveAsync(invRecord);
            _logger.LogInformation("Equipped item {ItemId} for character {CharId}", item.itemId, characterId);

            // 6. Trả về inventory đầy đủ
            return await GetInventoryAsync(characterId);
        }

        // =====================================================================
        // UNEQUIP ITEM (Mục 2.2 logic doc)
        // =====================================================================

        public async Task<InventoryResponse> UnequipItemAsync(string characterId, string inventoryId)
        {
            var invRecord = await _inventoryRepository.GetByInventoryIdAsync(inventoryId)
                ?? throw new GameNotFoundException("Không tìm thấy vật phẩm trong kho đồ.");

            if (invRecord.characterId != characterId)
                throw new GameValidationException("Vật phẩm không thuộc nhân vật này.");

            if (!invRecord.equipped)
                throw new GameValidationException("Vật phẩm hiện không được trang bị.");

            invRecord.equipped = false;
            await _inventoryRepository.SaveAsync(invRecord);
            _logger.LogInformation("Unequipped item {InvId} for character {CharId}", inventoryId, characterId);

            return await GetInventoryAsync(characterId);
        }

        // =====================================================================
        // USE ITEM — Consumable (Mục 2.4 logic doc)
        // =====================================================================

        public async Task<UseItemResponse> UseItemAsync(string characterId, string inventoryId, int quantityToUse)
        {
            if (quantityToUse <= 0)
                throw new GameValidationException("Số lượng sử dụng phải lớn hơn 0.");

            // 1. Lấy inventory record
            var invRecord = await _inventoryRepository.GetByInventoryIdAsync(inventoryId)
                ?? throw new GameNotFoundException("Không tìm thấy vật phẩm trong kho đồ.");

            if (invRecord.characterId != characterId)
                throw new GameValidationException("Vật phẩm không thuộc nhân vật này.");

            // 2. Kiểm tra item_type == Consumable
            var item = GameConstants.GetItemById(invRecord.itemId)
                ?? throw new GameNotFoundException($"Không tìm thấy thông tin item '{invRecord.itemId}' trong catalog.");

            if (item.itemType != "Consumable")
                throw new GameValidationException($"'{item.name}' không phải vật phẩm tiêu hao, không thể sử dụng.");

            // 3. Kiểm tra số lượng đủ
            if (invRecord.quantity < quantityToUse)
                throw new GameValidationException($"Không đủ số lượng (có {invRecord.quantity}, cần {quantityToUse}).");

            // 4. Lấy nhân vật để apply effect
            var character = await _characterRepository.GetByIdAsync(characterId)
                ?? throw new GameNotFoundException("Không tìm thấy nhân vật.");

            // 5. Trừ quantity
            invRecord.quantity -= quantityToUse;
            bool itemDeleted = invRecord.quantity == 0;

            if (itemDeleted)
                await _inventoryRepository.DeleteAsync(inventoryId);   // Mục 2.4: xóa khi hết
            else
                await _inventoryRepository.SaveAsync(invRecord);

            // 6. Apply effect từ effectJson (mục 2.4 bước 3)
            ApplyConsumableEffect(item.effectJson, character, quantityToUse);
            await _characterRepository.SaveAsync(character);

            _logger.LogInformation("Character {CharId} used {ItemName} x{Qty}. Deleted={Deleted}",
                characterId, item.name, quantityToUse, itemDeleted);

            return new UseItemResponse
            {
                inventoryId       = inventoryId,
                itemId            = item.itemId,
                itemName          = item.name,
                quantityRemaining = invRecord.quantity,
                itemDeleted       = itemDeleted,
                updatedStats      = new UpdatedCharacterStats
                {
                    hp           = character.hp,
                    maxHp        = character.maxHp,
                    attack       = character.attack,
                    defense      = character.defense,
                    criticalRate = character.criticalRate,
                    gold         = character.gold
                }
            };
        }

        // =====================================================================
        // GRANT LOOT DROP (Mục 5.2 logic doc)
        // =====================================================================

        public async Task<List<LootItemDTO>> GrantLootDropAsync(string characterId, string bossRarity, string battleId)
        {
            var results = new List<LootItemDTO>();

            // 1. Roll item rarity từ boss rarity (weighted random theo bảng loot)
            string itemRarity = GameConstants.RollItemRarity(bossRarity);

            // 2. Roll ngẫu nhiên item trong rarity đó (chỉ Equipment, không Consumable)
            var item = GameConstants.RollRandomItemByRarity(itemRarity);
            if (item == null)
            {
                _logger.LogWarning("No item found for rarity {Rarity} in catalog.", itemRarity);
                return results;
            }

            // 3. Kiểm tra capacity trước khi add (mục 2.5)
            try
            {
                await AddItemToInventoryAsync(characterId, item.itemId, 1);
            }
            catch (GameValidationException ex) when (ex.Message.Contains("Kho đồ đã đầy"))
            {
                _logger.LogWarning("Loot drop skipped for {CharId}: inventory full.", characterId);
                return results;
            }

            // 4. Ghi lịch sử LootDrop vào DynamoDB (mục 5.2.5)
            var lootRecord = new LootDrop
            {
                lootId     = Guid.NewGuid().ToString("N"),
                battleId   = battleId,
                itemId     = item.itemId,
                quantity   = 1,
                dropRate   = 1f,           // Đã được roll thành công
                sourceType = "Boss",
                isUnique   = item.rarity == "Legendary",
                createdAt  = DateTime.UtcNow
            };
            await _battleRepository.SaveLootDropAsync(lootRecord);

            results.Add(new LootItemDTO { itemId = item.itemId, quantity = 1 });

            _logger.LogInformation("Loot drop: {ItemName} ({Rarity}) for character {CharId}",
                item.name, item.rarity, characterId);

            return results;
        }

        // =====================================================================
        // PRIVATE HELPERS
        // =====================================================================

        /// <summary>
        /// Build InventorySlot đầy đủ (kèm thông tin item từ catalog).
        /// </summary>
        private static InventorySlot BuildInventorySlot(Inventory inv)
        {
            var item = GameConstants.GetItemById(inv.itemId);
            return new InventorySlot
            {
                inventoryId   = inv.inventoryId,
                itemId        = inv.itemId,
                itemName      = item?.name ?? inv.itemId,
                itemType      = item?.itemType ?? "",
                rarity        = item?.rarity ?? "",
                quantity      = inv.quantity,
                equipped      = inv.equipped,
                slotIndex     = inv.slotIndex,
                locked        = inv.locked,
                attackBonus   = item?.attackBonus   ?? 0,
                defenseBonus  = item?.defenseBonus  ?? 0,
                hpBonus       = item?.hpBonus       ?? 0,
                criticalBonus = item?.criticalBonus ?? 0f
            };
        }

        /// <summary>
        /// Áp dụng hiệu ứng từ effectJson lên nhân vật (mục 2.4 bước 3).
        /// Ví dụ effectJson: {"hp": 50} | {"hp_full": true}
        /// </summary>
        private static void ApplyConsumableEffect(string effectJson, Character character, int timesUsed)
        {
            if (string.IsNullOrWhiteSpace(effectJson)) return;

            try
            {
                using var doc = JsonDocument.Parse(effectJson);
                var root = doc.RootElement;

                // Hồi HP đầy
                if (root.TryGetProperty("hp_full", out var hpFull) && hpFull.GetBoolean())
                {
                    character.hp = character.maxHp;
                    return;
                }

                // Hồi HP theo lượng cố định
                if (root.TryGetProperty("hp", out var hpEl))
                {
                    int restore = hpEl.GetInt32() * timesUsed;
                    character.hp = Math.Min(character.hp + restore, character.maxHp);
                }

                // Tăng Attack tạm thời (ghi thẳng vào base stat — đơn giản hóa cho MVP)
                if (root.TryGetProperty("attack", out var atkEl))
                    character.attack += atkEl.GetInt32() * timesUsed;

                // Tăng Defense tạm thời
                if (root.TryGetProperty("defense", out var defEl))
                    character.defense += defEl.GetInt32() * timesUsed;

                // Thêm Gold
                if (root.TryGetProperty("gold", out var goldEl))
                    character.gold += goldEl.GetInt32() * timesUsed;
            }
            catch (JsonException)
            {
                // effectJson không hợp lệ — bỏ qua, không crash
            }
        }
    }
}
