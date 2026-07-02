# Kế Hoạch Triển Khai: Hệ Thống Item (Vật Phẩm)

Triển khai toàn bộ hệ thống logic vật phẩm (Item) cho game AI Dungeon RPG, căn chỉnh theo tài liệu [`logic_tam_thoi_cua_game.txt`](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/logic_tam_thoi_cua_game.txt). Phạm vi bao gồm: **Equip/Unequip**, **UseItem (Consumable)**, **Loot Drop** sau battle, **Inventory Capacity** và **LootDrop history log**.

> [!IMPORTANT]
> **Quyết định đã được xác nhận:**
> - ✅ Item Catalog: **Hardcode** trong `GameConstants.cs` (không cần DynamoDB table riêng).
> - ✅ UseItem (Consumable) **được thêm vào scope**.

---

## Những Điểm Mới/Thay Đổi So Với Plan Cũ (Căn Chỉnh Theo Logic Doc)

| # | Nội dung thay đổi | Nguồn từ logic doc |
|---|---|---|
| 1 | Thêm Boss rarity **Mythic** vào bảng loot drop | Mục 3.1 & 5.2 |
| 2 | Bảng loot drop sửa đúng số liệu (5 boss rarity × 4 item tier) | Mục 5.2 |
| 3 | Logic **Equip** dùng `item_type` (Weapon/Armor/Accessory) để kiểm tra slot conflict, không phải `slotType` | Mục 2.1 |
| 4 | Equip trả về stats nhân vật được **tính lại** (base + equipment bonus) | Mục 1.1 & 2.1 |
| 5 | **UseItem (Consume)**: kiểm tra `item_type == Consumable`, trừ quantity, xóa khi hết, apply effect lên Character | Mục 2.4 |
| 6 | **Inventory Capacity**: giới hạn **100 ô (Max_Slots)**, kiểm tra trước khi add item mới | Mục 2.5 |
| 7 | **Ghi nhận lịch sử** LootDrop vào bảng `LootDrop` sau mỗi battle Victory | Mục 5.2.5 |
| 8 | Công thức gold/XP reward dùng **Rarity Multiplier** (hệ số rarity) thay vì flat number | Mục 5.1 |
| 9 | `BossEncounter` model cần thêm field `bossRarity` để BattleService dùng khi tính loot | Mục 5.2 |

---

## Proposed Changes

### 1. Shared Layer — Models

#### [MODIFY] [Item.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/shared/Models/Item.cs)
Không thay đổi fields. Chỉ thêm XML doc comments mô tả từng field (không đổi schema).

#### [MODIFY] [BossEncounter.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/shared/Models/BossEncounter.cs)
Bổ sung field `bossRarity` (cần thiết để BattleService tính loot drop sau trận đấu):
```diff
+ public string bossRarity;   // "Common" | "Rare" | "Epic" | "Legendary" | "Mythic"
```

---

### 2. Shared Layer — DTOs

#### [NEW] `shared/DTOs/Inventory/ItemDetailResponse.cs`
DTO trả về chi tiết vật phẩm. Dùng bởi `GET /items/{itemId}`:
```csharp
public class ItemDetailResponse
{
    public string itemId;
    public string name;
    public string rarity;        // "Common" | "Rare" | "Epic" | "Legendary"
    public string itemType;      // "Weapon" | "Armor" | "Accessory" | "Consumable"
    public string slotType;      // "MainHand" | "Chest" | "Head" | "Legs" | "Ring" | "Neck"
    public int attackBonus;
    public int defenseBonus;
    public int hpBonus;
    public float criticalBonus;
    public string description;
    public string imageUrl;
    public bool stackable;
    public int sellPrice;
    public int buyPrice;
    public int requiredLevel;
    public string effectJson;    // Dành cho Consumable: {"hp": 50} hoặc {"attack": 10, "duration": 60}
}
```

#### [NEW] `shared/DTOs/Inventory/EquipItemRequest.cs`
```csharp
// POST /inventory/{characterId}/equip
public class EquipItemRequest  { public string inventoryId; }

// POST /inventory/{characterId}/unequip
public class UnequipItemRequest { public string inventoryId; }

// POST /inventory/{characterId}/use
public class UseItemRequest
{
    public string inventoryId;
    public int quantityToUse;   // Mặc định = 1 nếu không truyền
}
```

#### [NEW] `shared/DTOs/Inventory/UseItemResponse.cs`
```csharp
public class UseItemResponse
{
    public string inventoryId;
    public string itemId;
    public string itemName;
    public int quantityRemaining;
    public bool itemDeleted;             // true khi quantity = 0 sau khi dùng
    public UpdatedCharacterStats stats;  // Chỉ số nhân vật sau khi apply effect
}

public class UpdatedCharacterStats
{
    public int hp; public int maxHp;
    public int attack; public int defense;
    public float criticalRate;
}
```

#### [MODIFY] [InventoryResponse.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/shared/DTOs/Inventory/InventoryResponse.cs)
Bổ sung thông tin item vào `InventorySlot` để client hiển thị đủ:
```diff
public class InventorySlot
{
    public string inventoryId;
    public string itemId;
+   public string itemName;      // Lấy từ ItemCatalog khi build response
+   public string itemType;      // "Weapon" | "Armor" | "Consumable"
    public string rarity;
    public int quantity;
    public bool equipped;
    public int slotIndex;
    public bool locked;
+   public int attackBonus;      // Stats để hiển thị tooltip
+   public int defenseBonus;
+   public int hpBonus;
+   public float criticalBonus;
}
```

---

### 3. Backend Core — Config

#### [MODIFY] [GameConstants.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Core/Config/GameConstants.cs)

**A. Item Catalog (hardcode, phân theo rarity & type):**

| itemId | name | rarity | itemType | ATK | DEF | HP | CRIT |
|--------|------|--------|----------|-----|-----|----|------|
| `item_rusty_sword` | Rusty Sword | Common | Weapon | +3 | 0 | 0 | 0% |
| `item_leather_vest` | Leather Vest | Common | Armor | 0 | +5 | +10 | 0% |
| `item_health_potion_s` | Small Health Potion | Common | Consumable | 0 | 0 | 0 | 0% |
| `item_steel_dagger` | Steel Dagger | Rare | Weapon | +6 | 0 | 0 | +2% |
| `item_iron_shield` | Iron Shield | Rare | Armor | 0 | +10 | +15 | 0% |
| `item_health_potion_m` | Medium Health Potion | Rare | Consumable | 0 | 0 | 0 | 0% |
| `item_shadow_blade` | Shadow Blade | Epic | Weapon | +12 | 0 | 0 | +5% |
| `item_dragon_scale` | Dragon Scale Plate | Epic | Armor | 0 | +15 | +40 | 0% |
| `item_elixir` | Battle Elixir | Epic | Consumable | 0 | 0 | 0 | 0% |
| `item_excalibur` | Excalibur | Legendary | Weapon | +25 | 0 | +20 | +10% |
| `item_aegis` | Aegis of the Ancients | Legendary | Armor | 0 | +30 | +80 | 0% |

**B. Rarity Multipliers (dùng cho công thức gold/XP):**
```csharp
public static readonly Dictionary<string, (int GoldMod, int ExpMod)> RarityMultipliers = new()
{
    { "Common",    (goldMod: 10,  expMod: 15) },
    { "Rare",      (goldMod: 20,  expMod: 30) },
    { "Epic",      (goldMod: 40,  expMod: 60) },
    { "Legendary", (goldMod: 80,  expMod: 120) },
    { "Mythic",    (goldMod: 200, expMod: 300) }
};
// Gold = BossLevel × GoldMod + Random(10, 50)
// XP   = BossLevel × ExpMod
```

**C. Loot Drop Table (đúng số liệu từ logic doc):**

| Boss Rarity | Item Common | Item Rare | Item Epic | Item Legendary | Tổng |
|-------------|-------------|-----------|-----------|----------------|------|
| Common | 80% | 20% | 0% | 0% | 100% |
| Rare | 50% | 39% | 10% | 1% | 100% |
| Epic | 30% | 42% | 25% | 3% | 100% |
| Legendary | 18% | 29% | 44% | 9% | 100% |
| **Mythic** | 10% | 20% | 50% | **30%** | 100% |

> [!NOTE]
> Plan cũ thiếu boss **Mythic** và có tỉ lệ không khớp với logic doc. Đã sửa lại đúng theo file.

**D. Methods thêm vào `GameConstants`:**
```csharp
// Lấy item theo ID
public static Item? GetItemById(string itemId)

// Roll item rarity dựa theo boss rarity (dùng weighted random)
public static string RollItemRarity(string bossRarity)

// Roll item ngẫu nhiên trong rarity đã được chọn
public static Item? RollRandomItemByRarity(string itemRarity)

// Tính gold reward theo logic doc
public static int CalculateGoldReward(int bossLevel, string bossRarity)

// Tính XP reward theo logic doc
public static int CalculateExpReward(int bossLevel, string bossRarity)
```

---

### 4. Backend Core — Repository

#### [MODIFY] [IInventoryRepository.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Core/Repositories/Interfaces/IInventoryRepository.cs)
Bổ sung:
```csharp
// Lấy 1 inventory record theo PK
Task<Inventory?> GetByInventoryIdAsync(string inventoryId);

// Lấy danh sách item đang equipped của nhân vật
Task<List<Inventory>> GetEquippedItemsAsync(string characterId);

// Đếm số ô đang dùng (cho capacity check)
Task<int> CountSlotsAsync(string characterId);

// Xóa inventory record khi quantity = 0
Task DeleteAsync(string inventoryId);
```

#### [MODIFY] [InventoryRepository.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Core/Repositories/InventoryRepository.cs)
Triển khai:
- `GetByInventoryIdAsync`: `GetItemAsync(inventoryId)` theo Partition Key.
- `GetEquippedItemsAsync`: Scan với filter `characterId == X AND equipped == true`.
- `CountSlotsAsync`: Scan đếm số records có `characterId == X AND quantity > 0`.
- `DeleteAsync`: `DeleteItemAsync(inventoryId)`.

---

### 5. Backend Core — Service

#### [MODIFY] [IInventoryService.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Core/Services/Interfaces/IInventoryService.cs)
Thêm 4 method mới:
```csharp
// Lấy thông tin chi tiết item từ catalog
Task<ItemDetailResponse?> GetItemDetailAsync(string itemId);

// Trang bị item — tự unequip slot cũ cùng item_type
Task<InventoryResponse> EquipItemAsync(string characterId, string inventoryId);

// Gỡ trang bị item
Task<InventoryResponse> UnequipItemAsync(string characterId, string inventoryId);

// Dùng vật phẩm tiêu hao (Consumable)
Task<UseItemResponse> UseItemAsync(string characterId, string inventoryId, int quantityToUse);

// Cấp phát loot sau battle — gọi từ BattleService
Task<List<LootItemDTO>> GrantLootDropAsync(string characterId, string bossRarity);
```

#### [MODIFY] [InventoryService.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Core/Services/InventoryService.cs)

**`GetItemDetailAsync`**: Tra cứu từ `GameConstants.ItemCatalog`, map sang `ItemDetailResponse`.

**`EquipItemAsync`** (theo Mục 2.1 logic doc):
1. Lấy `Inventory` record theo `inventoryId`, verify thuộc `characterId`.
2. Kiểm tra `item.requiredLevel <= character.level`.
3. Tra cứu `item_type` của item trong `GameConstants.ItemCatalog`.
4. Gọi `_inventoryRepository.GetEquippedItemsAsync(characterId)`.
5. Nếu có item cùng `item_type` đang equipped → set `equipped = false`, save.
6. Set `equipped = true` cho item mới, save.
7. **Tính lại stats nhân vật** (base + tất cả equipment bonus đang equipped).
8. Trả về `InventoryResponse` đầy đủ.

**`UnequipItemAsync`** (theo Mục 2.2):
1. Lấy `Inventory` record theo `inventoryId`.
2. Kiểm tra `equipped == true`.
3. Set `equipped = false`, save.
4. Trả về `InventoryResponse` cập nhật.

**`UseItemAsync`** (theo Mục 2.4 logic doc):
1. Lấy `Inventory` record theo `inventoryId`, verify `characterId`.
2. Kiểm tra `item_type == "Consumable"` từ `GameConstants.ItemCatalog`.
3. Kiểm tra `quantity >= quantityToUse`.
4. Trừ quantity: `quantity -= quantityToUse`.
5. Nếu `quantity == 0` → gọi `_inventoryRepository.DeleteAsync(inventoryId)`.
6. Nếu `quantity > 0` → gọi `_inventoryRepository.SaveAsync(inventory)`.
7. **Apply effect** từ `item.effectJson` vào Character (ví dụ: `{"hp": 50}` → `character.hp = Math.Min(character.hp + 50, character.maxHp)`).
8. Lưu Character xuống DB.
9. Trả về `UseItemResponse` với stats mới.

**`GrantLootDropAsync`** (theo Mục 5.2 logic doc):
1. Gọi `GameConstants.RollItemRarity(bossRarity)` → lấy item rarity.
2. Gọi `GameConstants.RollRandomItemByRarity(itemRarity)` → lấy item.
3. Kiểm tra **Inventory Capacity** (Mục 2.5): `CountSlotsAsync(characterId)` → nếu `>= 100` và item chưa có → bỏ qua drop.
4. Gọi `AddItemToInventoryAsync(characterId, item.itemId, 1)`.
5. **Ghi LootDrop history** vào DynamoDB (loot_id, battle_id, item_id, quantity).
6. Trả về `List<LootItemDTO>`.

**`AddItemToInventoryAsync`** (cập nhật Mục 2.3 — thêm capacity check):
```diff
+ // Capacity check trước khi tạo bản ghi mới
+ var existing = await _inventoryRepository.FindByCharacterAndItemAsync(characterId, itemId);
+ if (existing == null)
+ {
+     int currentSlots = await _inventoryRepository.CountSlotsAsync(characterId);
+     if (currentSlots >= GameConstants.MaxInventorySlots)
+         throw new GameValidationException("Kho đồ đã đầy (100 ô).");
+ }
```

---

### 6. Backend Core — BattleService Integration

#### [MODIFY] [BattleService.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Core/Services/BattleService.cs)

**Thay đổi 1 — Lưu `bossRarity` vào `BossEncounter` khi spawn:**
```diff
var encounter = new BossEncounter
{
    encounterId = ...,
    characterId = ...,
    bossId      = ...,
    bossLevel   = bossLevel,
+   bossRarity  = rarity,       // Lưu để dùng khi resolve battle
    ...
};
```

**Thay đổi 2 — Cập nhật công thức Gold/XP theo logic doc:**
```diff
- int goldReward = encounter.bossLevel * 10;
- int expReward  = encounter.bossLevel * 15;
+ int goldReward = GameConstants.CalculateGoldReward(encounter.bossLevel, encounter.bossRarity);
+ int expReward  = GameConstants.CalculateExpReward(encounter.bossLevel, encounter.bossRarity);
```

**Thay đổi 3 — Tích hợp loot drop thật (thay thế list rỗng):**
```diff
- lootItems = new List<LootItemData>()   // ← hiện đang rỗng
+ var lootDTOs = await _inventoryService.GrantLootDropAsync(
+     character.characterId, encounter.bossRarity);
+ lootItems = lootDTOs.Select(l => new LootItemData
+ {
+     itemId   = l.itemId,
+     itemName = GameConstants.GetItemById(l.itemId)?.name ?? l.itemId,
+     quantity = l.quantity
+ }).ToList()
```

---

### 7. Backend Handlers — Lambda Entry Points

#### [MODIFY] [GetInventoryHandler.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Handlers/Handlers/Inventory/GetInventoryHandler.cs)
Không đổi logic, nhưng `InventorySlot` trong response bây giờ sẽ có thêm `itemName`, `itemType`, stats fields (do thay đổi ở `InventoryService.GetInventoryAsync`).

#### [NEW] `backend/.../Handlers/Inventory/GetItemDetailHandler.cs`
```
GET /items/{itemId}
```
- Gọi `IInventoryService.GetItemDetailAsync(itemId)`.
- Trả 404 nếu item không tìm thấy.

#### [NEW] `backend/.../Handlers/Inventory/EquipItemHandler.cs`
```
POST /inventory/{characterId}/equip
Body: { "inventoryId": "abc123" }
```

#### [NEW] `backend/.../Handlers/Inventory/UnequipItemHandler.cs`
```
POST /inventory/{characterId}/unequip
Body: { "inventoryId": "abc123" }
```

#### [NEW] `backend/.../Handlers/Inventory/UseItemHandler.cs`
```
POST /inventory/{characterId}/use
Body: { "inventoryId": "abc123", "quantityToUse": 1 }
```
- Validate `characterId`, `inventoryId`.
- Gọi `IInventoryService.UseItemAsync(...)`.
- Trả về `UseItemResponse`.

---

### 8. DI — ServiceProviderBuilder

#### [MODIFY] [ServiceProviderBuilder.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/backend/src/GameBackend.Handlers/DependencyInjection/ServiceProviderBuilder.cs)
Không cần thay đổi — `IInventoryService`, `IInventoryRepository` đã được đăng ký Singleton.

---

## File Summary

```
shared/
├── Models/
│   ├── [MODIFY] Item.cs                              ← thêm XML doc comments
│   └── [MODIFY] BossEncounter.cs                    ← thêm field bossRarity
├── DTOs/Inventory/
│   ├── [MODIFY] InventoryResponse.cs                ← bổ sung itemName, stats fields
│   ├── [NEW]    ItemDetailResponse.cs               ← DTO chi tiết item
│   ├── [NEW]    EquipItemRequest.cs                 ← request equip / unequip / use
│   └── [NEW]    UseItemResponse.cs                  ← response sau dùng đồ

backend/src/GameBackend.Core/
├── Config/
│   └── [MODIFY] GameConstants.cs                    ← ItemCatalog + LootTable + RarityMultiplier
├── Services/Interfaces/
│   └── [MODIFY] IInventoryService.cs                ← 5 method mới
├── Services/
│   └── [MODIFY] InventoryService.cs                 ← triển khai 5 method mới
│   └── [MODIFY] BattleService.cs                    ← fix loot, gold/xp công thức
├── Repositories/Interfaces/
│   └── [MODIFY] IInventoryRepository.cs             ← 4 method mới
└── Repositories/
    └── [MODIFY] InventoryRepository.cs              ← triển khai 4 method mới

backend/src/GameBackend.Handlers/Handlers/Inventory/
│   ├── [MODIFY] GetInventoryHandler.cs              ← không đổi logic
│   ├── [NEW]    GetItemDetailHandler.cs             ← GET /items/{itemId}
│   ├── [NEW]    EquipItemHandler.cs                 ← POST /inventory/{id}/equip
│   ├── [NEW]    UnequipItemHandler.cs               ← POST /inventory/{id}/unequip
│   └── [NEW]    UseItemHandler.cs                   ← POST /inventory/{id}/use
```

**Tổng: 5 file mới + 11 file sửa đổi**

---

## Verification Plan

### Build Check
```powershell
dotnet build shared/GameShared.csproj -c Debug
dotnet build backend/src/GameBackend.Core/GameBackend.Core.csproj -c Debug
dotnet build backend/src/GameBackend.Handlers/GameBackend.Handlers.csproj -c Debug
```

### Manual Verification

| Test Case | Expected |
|-----------|----------|
| `GET /items/item_excalibur` | 200 trả về stats đúng của Excalibur |
| `GET /items/not_exist` | 404 |
| `POST /inventory/{id}/equip` với inventoryId hợp lệ | `equipped = true`, slot cũ cùng item_type tự unequip |
| `POST /inventory/{id}/equip` khi nhân vật level thấp | 400 `LEVEL_REQUIRED` |
| `POST /inventory/{id}/use` với potion | HP nhân vật tăng, quantity giảm |
| `POST /inventory/{id}/use` khi hết potion | 400 `INSUFFICIENT_QUANTITY` |
| `POST /inventory/{id}/use` với item không phải Consumable | 400 `NOT_CONSUMABLE` |
| Battle Victory vs Mythic boss | lootItems không rỗng, xác suất Epic/Legendary cao |
| Battle Victory khi kho đầy 100 ô | Drop không được thêm, không crash |
| Kiểm tra `LootDrop` table | Có bản ghi mới sau mỗi battle Victory có drop |
| Công thức gold/XP | Boss Legendary Lv.10: gold = 10×80+Random(10,50), XP = 10×120 |
