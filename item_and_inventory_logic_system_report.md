# Báo Cáo Triển Khai: Item System & Inventory System
## Dự Án: AI Dungeon RPG Adventure Game
**Ngày thực hiện:** 30/06/2026  
**Phạm vi:** Backend (.NET 8 / AWS Lambda) + Shared Library  
**Tài liệu tham chiếu:** `logic_tam_thoi_cua_game.txt`

---

## Mục Lục

1. [Tổng Quan Hai Hệ Thống](#tong-quan)
2. [Phần I — Item System (Hệ Thống Vật Phẩm)](#item-system)
3. [Phần II — Inventory System (Hệ Thống Kho Đồ)](#inventory-system)
4. [Mối Liên Kết Giữa Hai Hệ Thống](#lien-ket)
5. [Kết Quả Build](#ket-qua-build)
6. [Lỗi Cũ Được Sửa](#loi-cu)
7. [Tổng Kết Số Liệu](#tong-ket)

---

## 1. Tổng Quan Hai Hệ Thống

Hai hệ thống này có ranh giới trách nhiệm rõ ràng nhưng hoạt động phối hợp chặt chẽ với nhau:

| Hệ thống | Trách nhiệm | Dữ liệu lưu trữ |
|----------|-------------|-----------------|
| **Item System** | Định nghĩa vật phẩm tồn tại trong game, tỉ lệ rơi đồ, thông tin stats | Hardcode trong `GameConstants` (không cần DB) |
| **Inventory System** | Quản lý những vật phẩm mà **từng nhân vật đang sở hữu**, thao tác trang bị/gỡ/dùng | DynamoDB table `GameInventory` |

> **Ví dụ minh họa:** "Shadow Blade" là một **Item** (tồn tại dưới dạng định nghĩa). Khi nhân vật A nhặt được nó sau trận đấu, một bản ghi **Inventory** được tạo ra ghi nhận "nhân vật A đang giữ Shadow Blade x1".

---

# PHẦN I — ITEM SYSTEM (Hệ Thống Vật Phẩm)

## 2.1. Tình Trạng Trước Khi Triển Khai

- `shared/Models/Item.cs` chỉ là một class rỗng với các fields chưa có bất kỳ logic nào.
- Không có danh sách vật phẩm nào (catalog) trong hệ thống.
- `BattleService` sau khi thắng trận trả về `lootItems = new List<LootItemData>()` — **danh sách rỗng**, không có vật phẩm nào thật sự rơi ra.
- Công thức tính thưởng Gold và XP dùng **số cứng** (`bossLevel × 10`, `bossLevel × 15`), không phân biệt độ hiếm Boss.

## 2.2. File Thay Đổi — Item System

| File | Loại | Nội dung thay đổi |
|------|------|-------------------|
| `shared/Models/BossEncounter.cs` | MODIFY | Thêm field `bossRarity` — cần lưu lại độ hiếm boss khi spawn để dùng khi tính loot/reward sau trận |
| `shared/DTOs/Inventory/ItemDetailResponse.cs` | **NEW** | DTO trả về thông tin chi tiết một vật phẩm (stats, description, price, effectJson) |
| `backend/.../Config/GameConstants.cs` | MODIFY | Thêm **Item Catalog**, **Loot Drop Table**, **Rarity Multipliers** và các helper method |
| `backend/.../Handlers/Inventory/GetItemDetailHandler.cs` | **NEW** | Lambda handler cho `GET /items/{itemId}` |
| `backend/.../Services/BattleService.cs` | MODIFY | Fix loot drop thật + cập nhật công thức Gold/XP theo hệ số rarity |

## 2.3. Item Catalog

**Vị trí:** `GameConstants.ItemCatalog`  
**Phương án:** Hardcode trong code (không cần DynamoDB table riêng) — tương tự cách `BossCatalog` đang hoạt động.

Tổng cộng **16 vật phẩm** phân theo 4 độ hiếm, mỗi độ hiếm có đủ 4 loại:

| Độ hiếm | Weapon | Armor | Accessory | Consumable |
|---------|--------|-------|-----------|------------|
| **Common** | Rusty Sword (+3 ATK) | Leather Vest (+5 DEF, +10 HP) | Wooden Ring (+1/+1/+5) | Small Health Potion (hồi 50 HP) |
| **Rare** | Steel Dagger (+6 ATK, +2% CRIT) | Iron Shield (+10 DEF, +15 HP) | Silver Amulet (+3/+3/+20, +1% CRIT) | Medium Health Potion (hồi 150 HP) |
| **Epic** | Shadow Blade (+12 ATK, +5% CRIT) | Dragon Scale Plate (+15 DEF, +40 HP) | Void Ring (+8/+5/+30, +3% CRIT) | Battle Elixir (hồi 400 HP) |
| **Legendary** | Excalibur (+25 ATK, +10% CRIT, +20 HP) | Aegis of the Ancients (+30 DEF, +80 HP) | Ring of the Gods (+15/+15/+60, +8% CRIT) | Divine Elixir (hồi đầy HP) |

Mỗi vật phẩm bao gồm: `itemId`, `name`, `rarity`, `itemType`, `slotType`, `attackBonus`, `defenseBonus`, `hpBonus`, `criticalBonus`, `requiredLevel`, `sellPrice`, `buyPrice`, `stackable`, `description`, `effectJson`.

## 2.4. Bảng Loot Drop (Tỉ Lệ Rơi Đồ)

**Vị trí:** `GameConstants.LootDropTable` + `GameConstants.RollItemRarity()`  
**Căn cứ:** Mục 5.2 — `logic_tam_thoi_cua_game.txt`

Cơ chế **Weighted Random**: sinh số ngẫu nhiên 0–99, tra bảng để xác định độ hiếm item rơi ra.

| Độ hiếm Boss | Item Common | Item Rare | Item Epic | Item Legendary | Tổng |
|---|:-:|:-:|:-:|:-:|:-:|
| Common (Thường) | 80% | 20% | 0% | 0% | 100% |
| Rare (Hiếm) | 50% | 39% | 10% | 1% | 100% |
| Epic (Sử thi) | 30% | 42% | 25% | 3% | 100% |
| Legendary (Huyền thoại) | 18% | 29% | 44% | 9% | 100% |
| Mythic (Thần thoại) | 10% | 20% | 50% | 30% | 100% |

> **Lưu ý thiết kế:** Vật phẩm rơi từ Boss chỉ thuộc loại **Equipment** (Weapon / Armor / Accessory). Consumable (potion) không rơi từ boss, chỉ có thể mua hoặc nhận từ nhiệm vụ.

## 2.5. Công Thức Thưởng Gold & XP

**Vị trí:** `GameConstants.RarityMultipliers`, `CalculateGoldReward()`, `CalculateExpReward()`  
**Căn cứ:** Mục 5.1 — `logic_tam_thoi_cua_game.txt`

```
Gold = BossLevel × GoldMod(Rarity) + Random(10, 50)
XP   = BossLevel × ExpMod(Rarity)
```

| Độ hiếm Boss | GoldMod | ExpMod | Ví dụ (Boss Lv.10) |
|---|:-:|:-:|---|
| Common | ×10 | ×15 | 110–160 Gold / 150 XP |
| Rare | ×20 | ×30 | 210–260 Gold / 300 XP |
| Epic | ×40 | ×60 | 410–460 Gold / 600 XP |
| Legendary | ×80 | ×120 | 810–860 Gold / 1200 XP |
| Mythic | ×200 | ×300 | 2010–2060 Gold / 3000 XP |

## 2.6. API Endpoint — Item

| Phương thức | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/items/{itemId}` | Tra cứu thông tin chi tiết và stats của vật phẩm |

---

# PHẦN II — INVENTORY SYSTEM (Hệ Thống Kho Đồ)

## 3.1. Tình Trạng Trước Khi Triển Khai

- `IInventoryService` chỉ có 2 method: `GetInventoryAsync` và `AddItemToInventoryAsync`.
- `IInventoryRepository` chỉ có 3 method: `GetByCharacterIdAsync`, `FindByCharacterAndItemAsync`, `SaveAsync`.
- `GetInventoryAsync` trả về `InventorySlot` **thiếu thông tin**: không có `itemName`, `itemType`, không có stats.
- `AddItemToInventoryAsync` **không kiểm tra giới hạn** số ô kho đồ — có thể thêm vô hạn item.
- Chưa có chức năng Trang bị (Equip), Gỡ trang bị (Unequip), Sử dụng vật phẩm (UseItem).

## 3.2. File Thay Đổi — Inventory System

| File | Loại | Nội dung thay đổi |
|------|------|-------------------|
| `shared/DTOs/Inventory/InventoryResponse.cs` | MODIFY | Bổ sung `itemName`, `itemType`, `attackBonus`, `defenseBonus`, `hpBonus`, `criticalBonus` vào `InventorySlot` |
| `shared/DTOs/Inventory/EquipItemRequest.cs` | **NEW** | 3 Request DTOs: `EquipItemRequest`, `UnequipItemRequest`, `UseItemRequest` |
| `shared/DTOs/Inventory/UseItemResponse.cs` | **NEW** | Response DTO sau khi dùng vật phẩm tiêu hao (quantity còn lại + stats mới của nhân vật) |
| `backend/.../Repositories/Interfaces/IInventoryRepository.cs` | MODIFY | Thêm 4 method mới |
| `backend/.../Repositories/InventoryRepository.cs` | MODIFY | Triển khai 4 method mới |
| `backend/.../Services/Interfaces/IInventoryService.cs` | MODIFY | Thêm 5 method mới |
| `backend/.../Services/InventoryService.cs` | MODIFY | Triển khai toàn bộ logic + inject thêm `ICharacterRepository`, `IBattleRepository` |
| `backend/.../Handlers/Inventory/EquipItemHandler.cs` | **NEW** | Lambda handler cho `POST /inventory/{characterId}/equip` |
| `backend/.../Handlers/Inventory/UnequipItemHandler.cs` | **NEW** | Lambda handler cho `POST /inventory/{characterId}/unequip` |
| `backend/.../Handlers/Inventory/UseItemHandler.cs` | **NEW** | Lambda handler cho `POST /inventory/{characterId}/use` |

## 3.3. Mở Rộng Repository Layer

### IInventoryRepository — 4 Method Mới

```csharp
// Lấy 1 bản ghi inventory theo Partition Key (inventoryId)
Task<Inventory?> GetByInventoryIdAsync(string inventoryId);

// Lấy danh sách tất cả item đang equipped của một nhân vật
Task<List<Inventory>> GetEquippedItemsAsync(string characterId);

// Đếm số ô đang dùng — cho capacity check (Max = 100)
Task<int> CountSlotsAsync(string characterId);

// Xóa bản ghi inventory (dùng khi quantity về 0 sau UseItem)
Task DeleteAsync(string inventoryId);
```

### Điểm Kỹ Thuật — `CountSlotsAsync`

Để tối ưu hiệu năng, `CountSlotsAsync` dùng **DynamoDB Projection** — chỉ đọc field `inventoryId` thay vì đọc toàn bộ bản ghi. Giảm đáng kể chi phí đọc (RCU) khi kho đồ có nhiều item.

## 3.4. Mở Rộng Service Layer

### IInventoryService — 5 Method Mới

```csharp
Task<ItemDetailResponse?> GetItemDetailAsync(string itemId);
Task<InventoryResponse>   EquipItemAsync(string characterId, string inventoryId);
Task<InventoryResponse>   UnequipItemAsync(string characterId, string inventoryId);
Task<UseItemResponse>     UseItemAsync(string characterId, string inventoryId, int quantityToUse);
Task<List<LootItemDTO>>   GrantLootDropAsync(string characterId, string bossRarity, string battleId);
```

## 3.5. Tính Năng: Trang Bị Vật Phẩm (Equip)

**API:** `POST /inventory/{characterId}/equip`  
**Căn cứ:** Mục 2.1 — `logic_tam_thoi_cua_game.txt`

Các bước xử lý:

| Bước | Hành động | DynamoDB Operation |
|------|-----------|-------------------|
| 1 | Lấy bản ghi `Inventory` theo `inventoryId`, xác nhận thuộc `characterId` | `GetItem` |
| 2 | Tra cứu `itemType` của item trong `ItemCatalog` (in-memory) | — |
| 3 | Lấy thông tin nhân vật để kiểm tra `requiredLevel` | `GetItem` |
| 4 | Tìm item cùng `itemType` đang equipped → gỡ ra (`equipped = false`) | `Scan` + `PutItem` |
| 5 | Set `equipped = true` cho item mới | `PutItem` |
| 6 | Trả về toàn bộ inventory đã cập nhật | `Scan` |

> **Quy tắc "1 slot 1 item":** Mỗi `itemType` chỉ được trang bị 1 item cùng lúc. Ví dụ: không thể mặc 2 Armor cùng lúc — hệ thống tự động gỡ cái cũ trước khi trang bị cái mới.

## 3.6. Tính Năng: Gỡ Trang Bị (Unequip)

**API:** `POST /inventory/{characterId}/unequip`  
**Căn cứ:** Mục 2.2 — `logic_tam_thoi_cua_game.txt`

Các bước xử lý:
1. Lấy bản ghi `Inventory` theo `inventoryId`.
2. Kiểm tra `equipped == true` — nếu đang không mặc → trả lỗi 400.
3. Set `equipped = false`, lưu xuống DynamoDB.
4. Trả về `InventoryResponse` cập nhật.

## 3.7. Tính Năng: Sử Dụng Vật Phẩm Tiêu Hao (Use Item)

**API:** `POST /inventory/{characterId}/use`  
**Căn cứ:** Mục 2.4 — `logic_tam_thoi_cua_game.txt`  
**Request Body:** `{ "inventoryId": "...", "quantityToUse": 1 }`

Các bước xử lý:

| Bước | Hành động |
|------|-----------|
| 1 | Lấy bản ghi Inventory, xác nhận quyền sở hữu |
| 2 | Kiểm tra `itemType == "Consumable"` — nếu không → lỗi 400 `NOT_CONSUMABLE` |
| 3 | Kiểm tra `quantity >= quantityToUse` — nếu không đủ → lỗi 400 `INSUFFICIENT_QUANTITY` |
| 4 | Trừ số lượng: `quantity -= quantityToUse` |
| 5 | Nếu `quantity == 0` → **xóa hẳn bản ghi** khỏi DynamoDB |
| 6 | Parse `effectJson` → Apply hiệu ứng lên nhân vật |
| 7 | Lưu nhân vật đã cập nhật xuống DynamoDB |
| 8 | Trả về `UseItemResponse` kèm stats nhân vật mới |

**Các hiệu ứng được hỗ trợ qua `effectJson`:**

| effectJson | Hiệu ứng |
|-----------|----------|
| `{"hp": 50}` | Hồi 50 HP (không vượt `maxHp`) |
| `{"hp_full": true}` | Hồi đầy HP |
| `{"attack": 10}` | Tăng Attack |
| `{"defense": 5}` | Tăng Defense |
| `{"gold": 100}` | Thêm Gold |

## 3.8. Tính Năng: Giới Hạn Sức Chứa Kho Đồ (Capacity)

**Vị trí:** `InventoryService.AddItemToInventoryAsync()` + `InventoryRepository.CountSlotsAsync()`  
**Căn cứ:** Mục 2.5 — `logic_tam_thoi_cua_game.txt`

**Quy tắc:**
- Mỗi nhân vật tối đa **100 ô** (`GameConstants.MaxInventorySlots = 100`).
- Mỗi `inventoryId` độc lập = **1 ô**, không phụ thuộc vào số lượng (`quantity`).

**Logic kiểm tra:**

```
Khi thêm item mới (item chưa có trong kho):
  → CountSlotsAsync() → đếm số bản ghi hiện tại
  → Nếu currentSlots >= 100 → Exception "Kho đồ đã đầy"
  → Nếu currentSlots < 100 → Tạo bản ghi mới

Khi thêm item đã có trong kho (cộng dồn):
  → Không tốn thêm ô, chỉ cập nhật quantity
  → Không kiểm tra capacity
```

## 3.9. Tính Năng: Nhận Loot Sau Trận Đấu (Grant Loot Drop)

**Vị trí:** `InventoryService.GrantLootDropAsync()`  
**Căn cứ:** Mục 5.2 — `logic_tam_thoi_cua_game.txt`

Luồng xử lý (được gọi tự động từ `BattleService` khi thắng trận):

```
1. Roll độ hiếm item (Weighted Random theo bossRarity)
2. Chọn ngẫu nhiên 1 item Equipment từ ItemCatalog
3. Kiểm tra capacity (100 ô) — nếu đầy → bỏ qua, không crash
4. AddItemToInventoryAsync() — thêm item vào kho nhân vật
5. SaveLootDropAsync() — ghi lịch sử vào DynamoDB table GameLootDrops
6. Trả về List<LootItemDTO> để BattleService đưa vào response
```

## 3.10. Cải Tiến GetInventoryAsync

`GetInventoryAsync` được cập nhật để trả về `InventorySlot` **đầy đủ hơn** — không chỉ trả về ID mà còn kèm thông tin từ ItemCatalog:

**Trước:**
```json
{
  "inventoryId": "abc123",
  "itemId": "item_shadow_blade",
  "quantity": 1,
  "equipped": true
}
```

**Sau:**
```json
{
  "inventoryId": "abc123",
  "itemId": "item_shadow_blade",
  "itemName": "Shadow Blade",
  "itemType": "Weapon",
  "rarity": "Epic",
  "quantity": 1,
  "equipped": true,
  "attackBonus": 12,
  "defenseBonus": 0,
  "hpBonus": 0,
  "criticalBonus": 0.05
}
```

## 3.11. API Endpoints — Inventory

| Phương thức | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/inventory/{characterId}` | Xem toàn bộ kho đồ (nâng cấp — có stats) |
| `POST` | `/inventory/{characterId}/equip` | Trang bị vật phẩm |
| `POST` | `/inventory/{characterId}/unequip` | Gỡ trang bị |
| `POST` | `/inventory/{characterId}/use` | Sử dụng vật phẩm tiêu hao |

---

## 4. Mối Liên Kết Giữa Hai Hệ Thống

```
                    ┌─────────────────────────────────┐
                    │         ITEM SYSTEM              │
                    │  GameConstants.ItemCatalog       │
                    │  (Hardcode — In-memory)          │
                    │                                  │
                    │  • 16 vật phẩm định nghĩa sẵn   │
                    │  • Loot Drop Table               │
                    │  • Rarity Multipliers            │
                    └───────────────┬─────────────────┘
                                    │ tra cứu thông tin item
                                    ▼
                    ┌─────────────────────────────────┐
                    │       INVENTORY SYSTEM           │
                    │  DynamoDB: GameInventory Table   │
                    │                                  │
                    │  • Bản ghi: nhân vật giữ item   │
                    │  • Trạng thái: equipped/locked   │
                    │  • Số lượng: quantity            │
                    │  • Giới hạn: 100 ô/nhân vật     │
                    └─────────────────────────────────┘
```

**Quy tắc tương tác:**
- `InventoryService` **luôn tra cứu `ItemCatalog`** để lấy thông tin item khi build response hoặc kiểm tra điều kiện (itemType, requiredLevel, effectJson).
- `ItemCatalog` **không bao giờ** đọc/ghi DynamoDB — chỉ là dữ liệu tĩnh trong bộ nhớ.
- Mọi thao tác người dùng (equip, use, v.v.) chỉ thay đổi bản ghi **Inventory**, không thay đổi định nghĩa **Item**.

---

## 5. Kết Quả Build

| Project | Kết quả | Errors | Warnings |
|---------|:-------:|:------:|:--------:|
| `GameShared.csproj` | ✅ | 0 | 152 *(nullable — có từ trước)* |
| `GameBackend.Core.csproj` | ✅ | 0 | 14 *(nullable — có từ trước)* |
| `GameBackend.Handlers.csproj` | ✅ | 0 | 0 |

---

## 6. Lỗi Cũ Được Sửa (Pre-existing Errors)

Trong quá trình build phát hiện 3 lỗi tồn tại sẵn, không liên quan tới tính năng mới:

| File | Lỗi | Cách sửa |
|------|-----|----------|
| `shared/Models/Character.cs` | Thiếu field `mp`, `maxMp` dù `CharacterService` đã dùng | Thêm 2 field vào model |
| `AIStory/Formatter/Impl/CharacterFormatter.cs` | Dùng `character.Name`, `character.HP` (PascalCase sai) | Sửa thành `character.name`, `character.hp` |
| `AIStory/Formatter/Impl/InventoryFormatter.cs` | Dùng `item.Name`, `item.Rarity` (PascalCase sai) | Sửa thành `item.name`, `item.rarity` |

---

## 7. Tổng Kết Số Liệu

### Item System

| Hạng mục | Số lượng |
|----------|:--------:|
| File mới tạo | 2 |
| File sửa đổi | 3 |
| Vật phẩm trong catalog | 16 |
| Boss tier trong loot table | 5 |
| API endpoint mới | 1 (`GET /items/{itemId}`) |

### Inventory System

| Hạng mục | Số lượng |
|----------|:--------:|
| File mới tạo | 5 |
| File sửa đổi | 6 |
| Method mới trong Repository | 4 |
| Method mới trong Service | 5 |
| API endpoint mới | 3 |
| API endpoint nâng cấp | 1 (`GET /inventory/{characterId}`) |

### Tổng Cộng

| Hạng mục | Tổng |
|----------|:----:|
| File mới tạo | **7** |
| File sửa đổi | **11** |
| API mới + nâng cấp | **5** |
| Dòng code mới (ước tính) | **~600** |
