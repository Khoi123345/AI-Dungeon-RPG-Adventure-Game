# Walkthrough — ProfileScene Setup trong Unity Editor

## Những gì đã được tạo

| File | Loại | Mục đích |
|---|---|---|
| [Character.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/shared/Models/Character.cs) | MODIFY | Thêm `speed`, `evasionRate`, `magicResist` |
| [CharacterTitle.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/shared/Models/CharacterTitle.cs) | NEW | Model danh hiệu |
| [ProfileCharacterData.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/shared/DTOs/Character/ProfileCharacterData.cs) | NEW | DTO tổng hợp |
| [GameProgressService.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Script/Core/GameProgressService.cs) | MODIFY | Thêm `GetTitles()`, `GetBattleHistory()`, `BuildProfileData()`, mock data |
| [ProfileModel.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Script/Profile/ProfileModel.cs) | NEW | Local view-model |
| [ProfilePresenter.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Script/Profile/ProfilePresenter.cs) | NEW | Controller MVP |
| [ProfileView.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Script/Profile/ProfileView.cs) | NEW | UI binding |
| [EquipmentSlotUI.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Script/Profile/EquipmentSlotUI.cs) | NEW | Prefab component slot trang bị |
| [TitleEntryUI.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Script/Profile/TitleEntryUI.cs) | NEW | Prefab component danh hiệu |
| [HistoryEntryUI.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Script/Profile/HistoryEntryUI.cs) | NEW | Prefab component lịch sử |
| [ProfileSceneLoader.cs](file:///d:/Unity/Project/AI-Dungeon-RPG-Adventure-Game/Assets/Script/Profile/ProfileSceneLoader.cs) | NEW | Nút mở Profile từ scene khác |

> [!NOTE]
> Build shared DLL đã thành công: **0 Errors, 178 Warnings** (warnings là nullable — pattern hiện có của dự án, không ảnh hưởng runtime).

---

## Bước 1 — Tạo ProfileScene trong Unity

1. `File > New Scene` → chọn template **Blank**
2. Lưu vào `Assets/Scenes/ProfileScene.unity`
3. **File > Build Settings** → click **Add Open Scenes** → đảm bảo `ProfileScene` xuất hiện trong danh sách

---

## Bước 2 — Hierarchy của ProfileScene

Tạo cấu trúc GameObjects sau trong Scene:

```
ProfileScene
└── Canvas (UI Canvas — Screen Space Overlay)
    ├── [GameObject] ProfileRoot          ← Attach ProfileView.cs + ProfilePresenter.cs
    │   ├── [Panel] Header
    │   │   ├── TMP: txtCharacterName
    │   │   ├── TMP: txtClassAndLevel
    │   │   ├── TMP: txtGold
    │   │   └── TMP: txtStatus
    │   │
    │   ├── [Panel] TabBar
    │   │   ├── Button: Tab_Overview      ← "Tổng Quan"
    │   │   ├── Button: Tab_Stats         ← "Chỉ Số"
    │   │   ├── Button: Tab_Equipment     ← "Trang Bị"
    │   │   ├── Button: Tab_Titles        ← "Danh Hiệu"
    │   │   └── Button: Tab_History       ← "Lịch Sử"
    │   │
    │   ├── [Panel] Content_Overview      ← Tab 0
    │   │   ├── Image: imgHPBar  (Type: Filled, FillMethod: Horizontal)
    │   │   ├── TMP: txtHP
    │   │   ├── Image: imgMPBar
    │   │   ├── TMP: txtMP
    │   │   ├── Image: imgEXPBar
    │   │   ├── TMP: txtEXP
    │   │   └── TMP: txtLocation
    │   │
    │   ├── [Panel] Content_Stats         ← Tab 1
    │   │   ├── TMP: txtAttack
    │   │   ├── TMP: txtDefense
    │   │   ├── TMP: txtCritRate
    │   │   ├── TMP: txtLuckyRate
    │   │   ├── TMP: txtSpeed
    │   │   ├── TMP: txtEvasion
    │   │   └── TMP: txtMagicResist
    │   │
    │   ├── [Panel] Content_Equipment     ← Tab 2
    │   │   └── [ScrollRect] + Content (Vertical Layout Group) ← equipmentSlotContainer
    │   │
    │   ├── [Panel] Content_Titles        ← Tab 3
    │   │   └── [ScrollRect] + Content (Vertical Layout Group) ← titleListContainer
    │   │
    │   ├── [Panel] Content_History       ← Tab 4
    │   │   └── [ScrollRect] + Content (Vertical Layout Group) ← historyListContainer
    │   │
    │   └── Button: btnClose              ← "← Quay Lại"
    │
└── EventSystem
```

---

## Bước 3 — Wire-up ProfileView Inspector

Chọn `ProfileRoot` GameObject → trong Inspector của **ProfileView**:

| Field | Kéo vào |
|---|---|
| `tabButtons[0..4]` | 5 tab buttons theo thứ tự |
| `tabPanels[0..4]` | 5 content panels theo thứ tự |
| Header fields | Các TMP text tương ứng |
| Tab 0 fields | imgHPBar, txtHP, imgMPBar, txtMP, imgEXPBar, txtEXP, txtLocation |
| Tab 1 fields | 7 TMP stats |
| `equipmentSlotContainer` | Content GameObject trong ScrollRect Tab 2 |
| `titleListContainer` | Content GameObject trong ScrollRect Tab 3 |
| `historyListContainer` | Content GameObject trong ScrollRect Tab 4 |
| `btnClose` | Close Button |

**ProfilePresenter** (cùng GameObject):
- Field `view` → kéo ProfileRoot (hoặc để trống, code tự `FindFirstObjectByType`)

---

## Bước 4 — Tạo 3 Prefabs

### Prefab: `EquipmentSlotUI`
1. Tạo `Assets/Prefabs/Profile/EquipmentSlotUI.prefab`
2. Root: Panel (Image background)
3. Thêm children TMP: `txtSlotType`, `txtItemName`, `txtRarity`, `txtStats`
4. Thêm `panelEmpty` (Panel với text "[Trống]")
5. Attach **EquipmentSlotUI.cs** → kéo references

### Prefab: `TitleEntryUI`
1. Tạo `Assets/Prefabs/Profile/TitleEntryUI.prefab`
2. Root: Panel + HorizontalLayoutGroup
3. Children: `txtTitleName`, `txtDescription`, `txtRarity`, `txtEarnedDate`
4. `iconEquipped`: một Image/Icon nhỏ (ẩn mặc định)
5. Attach **TitleEntryUI.cs** → kéo references

### Prefab: `HistoryEntryUI`
1. Tạo `Assets/Prefabs/Profile/HistoryEntryUI.prefab`
2. Root: Panel
3. Children: `txtBossName`, `txtResult`, `txtReward`, `txtDate`, `txtTurnCount`
4. `imgResultBadge`: Image nhỏ (dải màu bên trái)
5. Attach **HistoryEntryUI.cs** → kéo references

Sau khi tạo xong 3 prefabs:
- Kéo `EquipmentSlotUI` prefab vào `equipmentSlotPrefab` field của ProfileView
- Kéo `TitleEntryUI` prefab vào `titleEntryPrefab`
- Kéo `HistoryEntryUI` prefab vào `historyEntryPrefab`

---

## Bước 5 — Thêm nút Profile vào Menu Scene

Trong scene menu (hoặc StoryScene):
1. Tạo Button tên `BtnProfile`
2. Attach **ProfileSceneLoader.cs** lên Button đó
3. Field `profileSceneName`: nhập `"ProfileScene"` (phải khớp tên trong Build Settings)

Khi người chơi click nút → tự động load ProfileScene → nút Close trong Profile sẽ quay lại scene cũ.

---

## Kết quả khi chạy

ProfileScene sẽ hiển thị mock data từ `GameProgressService.SeedMockWorld()`:

| Tab | Dữ liệu mock |
|---|---|
| Tổng Quan | Dungeon Rider · Lv.7 · HP 84/120 · MP 30/40 · EXP 240/700 |
| Chỉ Số | ATK 18, DEF 8, CRIT 12%, LUCKY 8%, SPD 12, EVA 7%, MAG.RES 8 |
| Trang Bị | Weapon: Rusty Sword [Common] ATK+4, 5 slot còn trống |
| Danh Hiệu | 3 titles: 1 Epic, 1 Rare (equipped), 1 Common |
| Lịch Sử | 3 entries: Victory → Defeat → Victory (theo thứ tự mới nhất) |
