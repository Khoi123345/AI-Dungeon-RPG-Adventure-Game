# Báo Cáo Triển Khai: Hệ Thống Logic Vật Phẩm (Item System)
## Dự Án: AI Dungeon RPG Adventure Game
**Ngày thực hiện:** 30/06/2026  
**Phạm vi:** Backend (.NET 8 / AWS Lambda) + Shared Library

---

## 1. Tổng Quan

Trước khi triển khai, hệ thống vật phẩm của game chỉ có một model dữ liệu rỗng (`Item.cs`) trong thư mục `shared/Models/` mà chưa có bất kỳ logic nghiệp vụ nào. Cụ thể:

- `InventoryService` chỉ có 2 method cơ bản: lấy kho đồ và thêm item.
- Kết quả chiến đấu trả về danh sách loot **rỗng** (`new List<LootItemData>()`).
- Công thức tính thưởng Gold và XP sau trận đấu là **số cứng (flat number)**, không theo rarity của Boss.
- Chưa có chức năng Trang bị (Equip), Gỡ trang bị (Unequip), Sử dụng vật phẩm (Use Item).
- Chưa có giới hạn sức chứa kho đồ.

Sau khi triển khai, toàn bộ hệ thống vật phẩm hoạt động đầy đủ theo tài liệu thiết kế logic game (`logic_tam_thoi_cua_game.txt`).

---

## 2. Danh Sách File Thay Đổi

### 2.1. File Mới Tạo

| STT | File | Vị trí | Mô tả |
|-----|------|---------|-------|
| 1 | `ItemDetailResponse.cs` | `shared/DTOs/Inventory/` | DTO trả về thông tin chi tiết và chỉ số của một vật phẩm |
| 2 | `EquipItemRequest.cs` | `shared/DTOs/Inventory/` | Request DTOs cho 3 API: Trang bị / Gỡ trang bị / Sử dụng vật phẩm |
| 3 | `UseItemResponse.cs` | `shared/DTOs/Inventory/` | Response trả về sau khi sử dụng vật phẩm tiêu hao |
| 4 | `GetItemDetailHandler.cs` | `backend/.../Handlers/Inventory/` | Lambda handler cho `GET /items/{itemId}` |
| 5 | `EquipItemHandler.cs` | `backend/.../Handlers/Inventory/` | Lambda handler cho `POST /inventory/{characterId}/equip` |
| 6 | `UnequipItemHandler.cs` | `backend/.../Handlers/Inventory/` | Lambda handler cho `POST /inventory/{characterId}/unequip` |
| 7 | `UseItemHandler.cs` | `backend/.../Handlers/Inventory/` | Lambda handler cho `POST /inventory/{characterId}/use` |

### 2.2. File Sửa Đổi

| STT | File | Nội dung thay đổi |
|-----|------|-------------------|
| 1 | `shared/Models/BossEncounter.cs` | Thêm field `bossRarity` để lưu độ hiếm Boss khi tạo encounter |
| 2 | `shared/Models/Character.cs` | Thêm 2 field `mp` và `maxMp` (lỗi thiếu field từ trước) |
| 3 | `shared/DTOs/Inventory/InventoryResponse.cs` | Bổ sung các field `itemName`, `itemType`, `attackBonus`, `defenseBonus`, `hpBonus`, `criticalBonus` vào `InventorySlot` |
| 4 | `backend/.../Config/GameConstants.cs` | Thêm toàn bộ Item Catalog (16 vật phẩm), Loot Drop Table, Rarity Multipliers và các helper method |
| 5 | `backend/.../Repositories/Interfaces/IInventoryRepository.cs` | Thêm 4 method mới: `GetByInventoryIdAsync`, `GetEquippedItemsAsync`, `CountSlotsAsync`, `DeleteAsync` |
| 6 | `backend/.../Repositories/InventoryRepository.cs` | Triển khai đầy đủ 4 method mới |
| 7 | `backend/.../Services/Interfaces/IInventoryService.cs` | Thêm 5 method mới: `GetItemDetail`, `EquipItem`, `UnequipItem`, `UseItem`, `GrantLootDrop` |
| 8 | `backend/.../Services/InventoryService.cs` | Triển khai toàn bộ logic nghiệp vụ 5 method mới |
| 9 | `backend/.../Services/BattleService.cs` | Fix loot drop thật, cập nhật công thức Gold/XP theo hệ số rarity |
| 10 | `backend/.../AIStory/Formatter/Impl/CharacterFormatter.cs` | Fix lỗi pre-existing: đổi PascalCase → camelCase cho đúng field của model |
| 11 | `backend/.../AIStory/Formatter/Impl/InventoryFormatter.cs` | Fix lỗi pre-existing: đổi PascalCase → camelCase cho đúng field của model |

---

## 3. Chi Tiết Từng Tính Năng

### 3.1. Item Catalog (Danh Sách Vật Phẩm)

**Vị trí:** `GameConstants.cs`

Thay vì lưu vật phẩm trong database riêng (tốn tài nguyên AWS và phức tạp), danh sách vật phẩm được **hardcode trực tiếp trong code** tương tự cách Boss Catalog đang làm. Tổng cộng **16 vật phẩm** được định nghĩa theo 4 độ hiếm:

| Độ hiếm | Số lượng | Loại |
|---------|----------|------|
| Common (Thường) | 4 | Weapon, Armor, Accessory, Consumable |
| Rare (Hiếm) | 4 | Weapon, Armor, Accessory, Consumable |
| Epic (Sử thi) | 4 | Weapon, Armor, Accessory, Consumable |
| Legendary (Huyền thoại) | 4 | Weapon, Armor, Accessory, Consumable |

Mỗi vật phẩm bao gồm đầy đủ thông tin: `itemId`, `name`, `rarity`, `itemType`, `slotType`, các chỉ số tăng (`attackBonus`, `defenseBonus`, `hpBonus`, `criticalBonus`), `requiredLevel`, `sellPrice`, `buyPrice`, và `effectJson` (dành riêng cho vật phẩm tiêu hao).

### 3.2. Bảng Loot Drop (Tỉ Lệ Rơi Đồ)

**Vị trí:** `GameConstants.cs` — `LootDropTable` và method `RollItemRarity()`

Căn chỉnh đúng theo tài liệu `logic_tam_thoi_cua_game.txt` mục 5.2:

| Độ hiếm Boss | Item Common | Item Rare | Item Epic | Item Legendary | Tổng |
|---|---|---|---|---|---|
| Common (Thường) | 80% | 20% | 0% | 0% | 100% |
| Rare (Hiếm) | 50% | 39% | 10% | 1% | 100% |
| Epic (Sử thi) | 30% | 42% | 25% | 3% | 100% |
| Legendary (Huyền thoại) | 18% | 29% | 44% | 9% | 100% |
| Mythic (Thần thoại) | 10% | 20% | 50% | 30% | 100% |

Cơ chế **Weighted Random** được áp dụng: hệ thống sinh số ngẫu nhiên 0–99, sau đó tra bảng để xác định độ hiếm item rơi ra. Item rơi chỉ thuộc loại Equipment (Weapon / Armor / Accessory), **không** rơi Consumable từ boss.

### 3.3. Công Thức Thưởng Gold & XP

**Vị trí:** `GameConstants.cs` — `RarityMultipliers`, `CalculateGoldReward()`, `CalculateExpReward()`

Công thức được cập nhật theo mục 5.1 tài liệu logic game, thay thế số cứng cũ:

```
Gold = BossLevel × GoldMod(Rarity) + Random(10, 50)
XP   = BossLevel × ExpMod(Rarity)
```

| Độ hiếm Boss | GoldMod | ExpMod |
|---|---|---|
| Common | 10 | 15 |
| Rare | 20 | 30 |
| Epic | 40 | 60 |
| Legendary | 80 | 120 |
| Mythic | 200 | 300 |

**Ví dụ:** Boss Legendary cấp 10 → Gold = `10 × 80 + Random(10,50)` = **810–850 gold**, XP = `10 × 120` = **1200 XP**.

### 3.4. Hệ Thống Trang Bị (Equip / Unequip)

**Vị trí:** `InventoryService.EquipItemAsync()`, `InventoryService.UnequipItemAsync()`  
**API:** `POST /inventory/{characterId}/equip` | `POST /inventory/{characterId}/unequip`

Logic xử lý theo mục 2.1 và 2.2 tài liệu game:

**Equip Item:**
1. Lấy bản ghi `Inventory` theo `inventoryId`, kiểm tra quyền sở hữu của nhân vật.
2. Tra cứu thông tin item trong `ItemCatalog` để lấy `itemType`.
3. Kiểm tra điều kiện `requiredLevel` ≤ level nhân vật. Nếu không đủ cấp → trả lỗi 400 kèm thông báo.
4. Tìm kiếm item cùng `itemType` đang được trang bị (`equipped = true`) → tự động gỡ (set `equipped = false`).
5. Trang bị item mới (set `equipped = true`).
6. Trả về `InventoryResponse` cập nhật.

**Unequip Item:**
1. Kiểm tra `equipped == true`.
2. Set `equipped = false`.
3. Trả về `InventoryResponse` cập nhật.

### 3.5. Sử Dụng Vật Phẩm Tiêu Hao (Use Item / Consumable)

**Vị trí:** `InventoryService.UseItemAsync()`  
**API:** `POST /inventory/{characterId}/use`  
**Request Body:** `{ "inventoryId": "...", "quantityToUse": 1 }`

Logic xử lý theo mục 2.4 tài liệu game:

1. Kiểm tra `item_type == "Consumable"` → nếu không phải → trả lỗi 400.
2. Kiểm tra `quantity >= quantityToUse` → nếu không đủ → trả lỗi 400.
3. Trừ số lượng: `quantity -= quantityToUse`.
4. Nếu `quantity == 0` → **xóa hẳn bản ghi** khỏi DynamoDB.
5. **Áp dụng hiệu ứng** (`effectJson`) lên nhân vật:
   - `{"hp": 50}` → Cộng 50 HP (không vượt quá `maxHp`).
   - `{"hp_full": true}` → Hồi đầy HP.
   - `{"attack": N}` → Tăng Attack.
   - `{"gold": N}` → Thêm Gold.
6. Lưu nhân vật xuống DynamoDB và trả về `UseItemResponse` kèm chỉ số mới.

### 3.6. Giới Hạn Sức Chứa Kho Đồ (Inventory Capacity)

**Vị trí:** `InventoryService.AddItemToInventoryAsync()`, `InventoryRepository.CountSlotsAsync()`

Căn chỉnh theo mục 2.5 tài liệu game:

- **Giới hạn:** Mỗi nhân vật tối đa **100 ô** (`MaxInventorySlots = 100`).
- Mỗi `inventoryId` độc lập chiếm **1 ô**, không phụ thuộc vào `quantity`.
- Khi thêm item **đã có** trong kho → chỉ cộng dồn `quantity`, **không tốn thêm ô mới**.
- Khi thêm item **chưa có** → đếm số ô hiện tại. Nếu `>= 100` → trả về lỗi *"Kho đồ đã đầy"*.

Phương thức `CountSlotsAsync()` dùng DynamoDB **Projection** (chỉ đọc field `inventoryId`) để tiết kiệm tài nguyên đọc (RCU).

### 3.7. Hệ Thống Loot Drop Sau Chiến Đấu

**Vị trí:** `InventoryService.GrantLootDropAsync()`, tích hợp vào `BattleService.ResolveBattleAsync()`

Luồng xử lý khi thắng battle:
1. `BattleService` gọi `GrantLootDropAsync(characterId, bossRarity, battleId)`.
2. `GrantLootDropAsync` roll độ hiếm item từ `LootDropTable` theo `bossRarity`.
3. Chọn ngẫu nhiên 1 item trong `ItemCatalog` khớp độ hiếm đã roll.
4. Kiểm tra capacity (100 ô) — nếu đầy thì bỏ qua drop, không crash.
5. Thêm item vào kho đồ nhân vật.
6. **Ghi lịch sử** vào DynamoDB table `GameLootDrops` (mục 5.2.5 tài liệu).
7. Trả về danh sách `LootItemDTO` để `BattleService` đưa vào `BattleResolveResponse`.

### 3.8. API Tra Cứu Thông Tin Vật Phẩm

**Vị trí:** `GetItemDetailHandler.cs`  
**API:** `GET /items/{itemId}`

Tra cứu trực tiếp từ `GameConstants.ItemCatalog` trong bộ nhớ (không cần DynamoDB query). Trả về `ItemDetailResponse` đầy đủ stats. Nếu `itemId` không tồn tại → trả 404.

---

## 4. Kiến Trúc & Luồng Dữ Liệu

### 4.1. Luồng Equip Item

```
Unity Client
  → POST /inventory/{characterId}/equip  { inventoryId }
    → EquipItemHandler (parse request, validate)
      → InventoryService.EquipItemAsync()
          → IInventoryRepository.GetByInventoryIdAsync()    [DynamoDB GetItem]
          → GameConstants.GetItemById()                     [In-memory lookup]
          → ICharacterRepository.GetByIdAsync()             [DynamoDB GetItem]
          → IInventoryRepository.GetEquippedItemsAsync()    [DynamoDB Scan]
          → IInventoryRepository.SaveAsync() × N            [DynamoDB PutItem]
          → InventoryService.GetInventoryAsync()            [Build response]
      ← InventoryResponse (danh sách kho đồ cập nhật)
    ← APIGatewayProxyResponse (200 OK, JSON)
  ← Cập nhật UI kho đồ
```

### 4.2. Luồng Loot Drop Sau Battle

```
BattleService.ResolveBattleAsync()
  → Tính toán kết quả trận đấu
  → [Nếu Victory]
      → GameConstants.CalculateGoldReward(bossLevel, bossRarity)
      → GameConstants.CalculateExpReward(bossLevel, bossRarity)
      → CharacterService.ApplyExperienceAndLevelUp()
      → InventoryService.GrantLootDropAsync(characterId, bossRarity, battleId)
          → GameConstants.RollItemRarity(bossRarity)         [Weighted random]
          → GameConstants.RollRandomItemByRarity(itemRarity) [Random from catalog]
          → InventoryRepository.CountSlotsAsync()            [Capacity check]
          → InventoryService.AddItemToInventoryAsync()
          → BattleRepository.SaveLootDropAsync()             [Ghi lịch sử]
      ← List<LootItemDTO>
  ← BattleResolveResponse { lootItems, goldEarned, expEarned, turns, ... }
```

---

## 5. API Endpoints Mới

| Phương thức | Endpoint | Mô tả | Handler |
|---|---|---|---|
| `GET` | `/items/{itemId}` | Lấy thông tin chi tiết vật phẩm | `GetItemDetailHandler` |
| `POST` | `/inventory/{characterId}/equip` | Trang bị vật phẩm | `EquipItemHandler` |
| `POST` | `/inventory/{characterId}/unequip` | Gỡ trang bị vật phẩm | `UnequipItemHandler` |
| `POST` | `/inventory/{characterId}/use` | Sử dụng vật phẩm tiêu hao | `UseItemHandler` |
| `GET` | `/inventory/{characterId}` | Xem kho đồ *(đã có, nâng cấp thêm stats)* | `GetInventoryHandler` |

---

## 6. Kết Quả Build

Sau khi hoàn tất triển khai, toàn bộ 3 project trong solution biên dịch thành công:

| Project | Kết quả | Lỗi | Cảnh báo |
|---------|---------|-----|---------|
| `GameShared.csproj` | ✅ Thành công | 0 | 152 (nullable — có từ trước) |
| `GameBackend.Core.csproj` | ✅ Thành công | 0 | 14 (nullable — có từ trước) |
| `GameBackend.Handlers.csproj` | ✅ Thành công | 0 | 0 |

> **Ghi chú:** Toàn bộ cảnh báo (warnings) về `nullable reference` là warnings đã tồn tại từ trước trong codebase, không phải do code mới gây ra. Đây là convention sử dụng `public fields` thay vì `properties` để tương thích với Unity `JsonUtility` serialization.

---

## 7. Lỗi Cũ Được Sửa (Pre-existing Errors)

Trong quá trình build, phát hiện và sửa 3 lỗi tồn tại sẵn trong codebase:

| File | Lỗi | Cách sửa |
|------|-----|----------|
| `shared/Models/Character.cs` | Thiếu field `mp` và `maxMp` mặc dù `CharacterService` đã dùng | Thêm 2 field vào model |
| `AIStory/Formatter/Impl/CharacterFormatter.cs` | Dùng `character.Name`, `character.HP`... (PascalCase) trong khi model dùng camelCase | Đổi sang `character.name`, `character.hp`... |
| `AIStory/Formatter/Impl/InventoryFormatter.cs` | Dùng `item.Name`, `item.Rarity` (PascalCase) | Đổi sang `item.name`, `item.rarity` |

---

## 8. Tổng Kết

| Hạng mục | Số lượng |
|----------|----------|
| File mới tạo | 7 |
| File sửa đổi | 11 |
| API endpoint mới | 4 |
| Vật phẩm trong catalog | 16 |
| Boss tier trong loot table | 5 (Common → Mythic) |
| Dòng code mới (ước tính) | ~600 dòng |

Hệ thống vật phẩm đã được triển khai đầy đủ theo thiết kế, đảm bảo:
- **Anti-cheat:** Toàn bộ tính toán (loot roll, capacity check, effect apply) diễn ra ở **server** (Lambda), client không thể can thiệp.
- **Consistency:** Dùng chung `GameConstants.ItemCatalog` cho cả loot drop lẫn equip/use — không bao giờ bất đồng bộ dữ liệu.
- **Scalability:** Khi cần thêm item mới, chỉ việc thêm vào `ItemCatalog` trong `GameConstants.cs` — không cần thay đổi database schema.
