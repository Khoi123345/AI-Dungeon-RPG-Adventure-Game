using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("--- UI References ---")]
    public Transform gridSlotsContainer; 
    public TMP_Dropdown dropdownFilter;
    public TextMeshProUGUI txtAtk;       // Text (TMP) (1) — Tấn công
    public TextMeshProUGUI txtDef;       // Text (TMP) (2) — Phòng thủ
    public TextMeshProUGUI txtPageNumber;
    public Button btnClose;

    [Header("--- Equipment Slots (Left Panel) ---")]
    public InventorySlotUI slotHelmet;
    public InventorySlotUI slotArmor;
    public InventorySlotUI slotAccessory;
    public InventorySlotUI slotWeapon;

    [Header("--- Pagination Buttons ---")]
    public Button btnFirst;
    public Button btnPrev;
    public Button btnNext;
    public Button btnLast;

    [Header("--- Templates & Database ---")]
    [SerializeField] private List<ItemData> itemDatabase = new List<ItemData>();

    [Header("--- Inventory Data ---")]
    public List<ItemData> allItems = new List<ItemData>(); // Danh sách toàn bộ item đang có
    private List<ItemData> filteredItems = new List<ItemData>(); // Danh sách sau khi lọc (chỉ chứa món chưa trang bị)

    private int currentPage = 1;
    private int itemsPerPage = 49; // Đúng bằng số ô vuông trên 1 trang
    private int totalPages = 1;

    void Start()
    {
        // Gán sự kiện cho Dropdown, Nút phân trang và Nút Đóng UI
        if (dropdownFilter != null)
        {
            dropdownFilter.onValueChanged.RemoveAllListeners();
            dropdownFilter.onValueChanged.AddListener(OnFilterChanged);
        }

        if (btnFirst != null)
        {
            btnFirst.onClick.RemoveAllListeners();
            btnFirst.onClick.AddListener(() => ChangePage(1));
        }
        if (btnPrev != null)
        {
            btnPrev.onClick.RemoveAllListeners();
            btnPrev.onClick.AddListener(() => ChangePage(currentPage - 1));
        }
        if (btnNext != null)
        {
            btnNext.onClick.RemoveAllListeners();
            btnNext.onClick.AddListener(() => ChangePage(currentPage + 1));
        }
        if (btnLast != null)
        {
            btnLast.onClick.RemoveAllListeners();
            btnLast.onClick.AddListener(() => ChangePage(totalPages));
        }
        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(CloseInventoryPanel);
        }

        AutoFindEquipmentSlots();
        RefreshInventoryUI();
    }

    private void OnEnable()
    {
        AutoFindEquipmentSlots();
        RefreshInventoryUI();
    }

    public void CloseInventoryPanel()
    {
        // 🛡️ CHỐNG TẮT GAMEMANAGER: Chỉ tắt duy nhất Panel_Inventory!
        GameObject panelObj = null;

        if (this.gameObject.name != "GameManager" && this.gameObject.name.Contains("Inventory"))
        {
            panelObj = this.gameObject;
        }
        else
        {
            Transform panelTr = transform.Find("Panel_Inventory");
            if (panelTr == null && transform.parent != null && transform.parent.name.Contains("Inventory"))
                panelTr = transform.parent;
            if (panelTr != null) panelObj = panelTr.gameObject;
            else panelObj = GameObject.Find("Panel_Inventory");
        }

        if (panelObj != null && panelObj.name != "GameManager")
        {
            panelObj.SetActive(false);
            Debug.Log("🚪 [INVENTORY CLOSED] Đã ẩn giao diện Panel_Inventory (GameManager vẫn hoạt động).");
        }
        else
        {
            Debug.LogWarning("[InventoryManager] Không thể tắt Panel_Inventory vì tránh làm ngắt kết nối GameManager.");
        }
    }

    private void AutoFindEquipmentSlots()
    {
        Transform leftPanel = transform.Find("Left_CharacterPanel");
        if (leftPanel == null && transform.parent != null)
            leftPanel = transform.parent.Find("Left_CharacterPanel");
        if (leftPanel == null)
            leftPanel = GameObject.Find("Left_CharacterPanel")?.transform;

        if (leftPanel != null)
        {
            Transform equipGroup = leftPanel.Find("Equipment_Slots");
            if (equipGroup != null)
            {
                if (slotHelmet == null) slotHelmet = GetOrAddSlotUI(equipGroup.Find("Slot_Helmet"));
                if (slotArmor == null) slotArmor = GetOrAddSlotUI(equipGroup.Find("Slot_Armor"));
                if (slotAccessory == null) slotAccessory = GetOrAddSlotUI(equipGroup.Find("Slot_Accessory"));
                if (slotWeapon == null) slotWeapon = GetOrAddSlotUI(equipGroup.Find("Slot_Weapon"));
            }
        }
    }

    private InventorySlotUI GetOrAddSlotUI(Transform slotTr)
    {
        if (slotTr == null) return null;
        InventorySlotUI slotUI = slotTr.GetComponent<InventorySlotUI>();
        if (slotUI == null)
        {
            slotUI = slotTr.gameObject.AddComponent<InventorySlotUI>();
        }
        return slotUI;
    }

    public void RefreshInventoryUI()
    {
        AutoFindEquipmentSlots();

        // 1. Xóa hiển thị 4 ô trang bị bên trái
        if (slotHelmet != null) slotHelmet.ClearSlot();
        if (slotArmor != null) slotArmor.ClearSlot();
        if (slotAccessory != null) slotAccessory.ClearSlot();
        if (slotWeapon != null) slotWeapon.ClearSlot();

        // 2. Nạp dữ liệu từ GameProgressService
        LoadInventoryFromProgressService();

        // 3. Đưa các món ĐÃ TRANG BỊ lên 4 ô tương ứng bên trái (Left Panel)
        foreach (var item in allItems)
        {
            if (item.isEquipped)
            {
                InventorySlotUI targetLeftSlot = GetTargetEquipmentSlot(item, item.itemName);
                if (targetLeftSlot != null)
                {
                    targetLeftSlot.AddItemToSlot(item, item.quantity);
                    targetLeftSlot.SetEquipped(true);

                    string capturedInvId = item.inventoryId;
                    targetLeftSlot.onSlotClicked = (clickedSlot) =>
                    {
                        if (clickedSlot.hasItem && !string.IsNullOrEmpty(capturedInvId))
                        {
                            GameProgressService.Instance?.ToggleEquipItemByInventoryId(capturedInvId);
                            RefreshInventoryUI();
                        }
                    };
                }
            }
        }

        // 4. Áp dụng bộ lọc và render các món CHƯA TRANG BỊ sang lưới bên phải (Right Grid)
        ApplyFilter();
    }

    private InventorySlotUI GetTargetEquipmentSlot(ItemData item, string itemIdOrName)
    {
        var template = GameShared.Config.GameConstants.GetItemById(itemIdOrName);
        string slotType = template?.slotType?.ToLower() ?? "";
        string lowerId = (itemIdOrName ?? "").ToLower();

        if (slotType.Contains("helmet") || slotType.Contains("head") || lowerId.Contains("helmet") || lowerId.Contains("cap") || lowerId.Contains("crown") || lowerId.Contains("hood"))
        {
            return slotHelmet;
        }

        if (item.itemType == ItemType.Armor || slotType.Contains("armor") || lowerId.Contains("armor") || lowerId.Contains("vest") || lowerId.Contains("plate"))
        {
            return slotArmor;
        }

        if (item.itemType == ItemType.Accessory || slotType.Contains("accessory") || lowerId.Contains("ring") || lowerId.Contains("amulet") || lowerId.Contains("necklace"))
        {
            return slotAccessory;
        }

        if (item.itemType == ItemType.Weapon || slotType.Contains("weapon") || lowerId.Contains("sword") || lowerId.Contains("bow") || lowerId.Contains("staff") || lowerId.Contains("shield") || lowerId.Contains("blade"))
        {
            return slotWeapon;
        }

        return slotWeapon; // Mặc định
    }

    private void LoadInventoryFromProgressService()
    {
        allItems.Clear();
        if (GameProgressService.Instance == null) return;
        var inventoryItems = GameProgressService.Instance.GetInventory();
        if (inventoryItems == null || inventoryItems.Count == 0) return;

        foreach (var inv in inventoryItems)
        {
            var template = GameShared.Config.GameConstants.GetItemById(inv.itemId);

            ItemData dbMatch = null;
            if (itemDatabase != null && itemDatabase.Count > 0)
            {
                dbMatch = itemDatabase.Find(x => x != null && 
                    !string.IsNullOrEmpty(x.itemName) &&
                    (x.itemName.Equals(inv.itemId, System.StringComparison.OrdinalIgnoreCase) ||
                     (template != null && x.itemName.Equals(template.name, System.StringComparison.OrdinalIgnoreCase))));
            }

            // Tạo bản sao độc lập duy nhất cho từng món trong túi đồ!
            ItemData matchData = new ItemData
            {
                itemName = dbMatch != null && !string.IsNullOrEmpty(dbMatch.itemName) 
                            ? dbMatch.itemName 
                            : (template != null ? template.name : (string.IsNullOrEmpty(inv.itemId) ? "Inventory Item" : inv.itemId)),
                itemIcon = dbMatch?.itemIcon,
                itemType = template != null && System.Enum.TryParse<ItemType>(template.itemType, true, out var parsedType) 
                            ? parsedType 
                            : ItemData.GetItemTypeFromId(inv.itemId),
                atkBonus = dbMatch != null && dbMatch.atkBonus != 0 ? dbMatch.atkBonus : (template != null ? template.attackBonus : 0),
                defBonus = dbMatch != null && dbMatch.defBonus != 0 ? dbMatch.defBonus : (template != null ? template.defenseBonus : 0),
                itemRarity = dbMatch != null ? dbMatch.itemRarity : (template != null && System.Enum.TryParse<ItemRarity>(template.rarity, true, out var parsedRarity) ? parsedRarity : ItemRarity.Common),
                quantity = inv.quantity,
                inventoryId = !string.IsNullOrEmpty(inv.inventoryId) ? inv.inventoryId : inv.itemId,
                isEquipped = inv.equipped
            };

            allItems.Add(matchData);
        }
    }

    public void LoadItems(List<ItemData> items)
    {
        if (items == null || items.Count == 0) return;
        allItems = items;
        currentPage = 1;
        ApplyFilter();
    }

    void OnFilterChanged(int value)
    {
        currentPage = 1; // Về trang 1 khi đổi bộ lọc
        ApplyFilter();
    }

    public void ApplyFilter()
    {
        filteredItems.Clear();

        int filterIndex = dropdownFilter != null ? dropdownFilter.value : 0; // 0: All, 1: Weapon, 2: Armor, 3: Accessory, 4: Consumable

        // 🎯 LỌC DUY NHẤT CÁC MÓN CHƯA TRANG BỊ (!item.isEquipped) ĐỂ HIỂN THỊ BÊN PHẢI!
        List<ItemData> unequippedItems = allItems.FindAll(item => !item.isEquipped);

        if (filterIndex == 0)
        {
            filteredItems.AddRange(unequippedItems);
        }
        else
        {
            ItemType selectedType = (ItemType)(filterIndex - 1);
            filteredItems = unequippedItems.FindAll(item => item.itemType == selectedType);
        }

        // Tính tổng số trang
        totalPages = Mathf.CeilToInt((float)filteredItems.Count / itemsPerPage);
        if (totalPages < 1) totalPages = 1;

        RenderPage();
    }

    void ChangePage(int newPage)
    {
        currentPage = Mathf.Clamp(newPage, 1, totalPages);
        RenderPage();
    }

    void RenderPage()
    {
        // --- Cập nhật Text trang ---
        if (txtPageNumber != null)
            txtPageNumber.text = $"{currentPage} / {totalPages}";

        // --- Khóa/Mở nút bấm phân trang ---
        if (btnFirst != null) btnFirst.interactable = (currentPage > 1);
        if (btnPrev  != null) btnPrev.interactable  = (currentPage > 1);
        if (btnNext  != null) btnNext.interactable  = (currentPage < totalPages);
        if (btnLast  != null) btnLast.interactable  = (currentPage < totalPages);

        if (gridSlotsContainer == null) return;

        // --- Hiển thị item CHƯA TRANG BỊ lên từng ô Slot bên phải ---
        int startIndex = (currentPage - 1) * itemsPerPage;
        int slotCount  = gridSlotsContainer.childCount;

        for (int i = 0; i < slotCount; i++)
        {
            Transform slot = gridSlotsContainer.GetChild(i);
            InventorySlotUI slotUI = GetOrAddSlotUI(slot);

            int itemIndex = startIndex + i;

            if (itemIndex < filteredItems.Count)
            {
                ItemData item = filteredItems[itemIndex];

                if (slotUI != null)
                {
                    slotUI.AddItemToSlot(item, item.quantity);
                    slotUI.SetEquipped(false);

                    string capturedInvId = item.inventoryId;
                    slotUI.onSlotClicked = (clickedSlot) =>
                    {
                        if (clickedSlot.hasItem && !string.IsNullOrEmpty(capturedInvId))
                        {
                            GameProgressService.Instance?.ToggleEquipItemByInventoryId(capturedInvId);
                            RefreshInventoryUI();
                        }
                    };
                }
                slot.gameObject.SetActive(true);
            }
            else
            {
                if (slotUI != null)
                {
                    slotUI.ClearSlot();
                }
                slot.gameObject.SetActive(true); // Giữ ô hiển thị để có màu nền tối đẹp mắt
            }
        }

        // --- Cập nhật bảng Stats ---
        UpdateStatsDisplay();
    }

    // Tính tổng ATK + DEF từ toàn bộ item trong túi rồi hiển thị
    void UpdateStatsDisplay()
    {
        int totalAtk = 0;
        int totalDef = 0;

        if (GameProgressService.Instance != null && GameProgressService.Instance.CurrentCharacter != null)
        {
            totalAtk = GameProgressService.Instance.CurrentCharacter.attack;
            totalDef = GameProgressService.Instance.CurrentCharacter.defense;
        }
        else
        {
            foreach (ItemData item in allItems)
            {
                if (item.isEquipped)
                {
                    totalAtk += item.atkBonus;
                    totalDef += item.defBonus;
                }
            }
        }

        if (txtAtk != null) txtAtk.text = $"Tấn công: {totalAtk}";
        if (txtDef != null) txtDef.text = $"Phòng thủ: {totalDef}";
    }

    /// <summary>
    /// Thêm vật phẩm vào túi đồ (được gọi khi gỡ trang bị hoặc hoán đổi).
    /// </summary>
    public void AddItemToInventory(ItemData item)
    {
        if (item == null) return;

        // Kiểm tra xem item đã tồn tại chưa (so sánh inventoryId)
        var existing = allItems.Find(x => x != null && !string.IsNullOrEmpty(x.inventoryId)
                                          && x.inventoryId == item.inventoryId);
        if (existing != null)
        {
            existing.quantity += item.quantity;
        }
        else
        {
            item.isEquipped = false;
            allItems.Add(item);
        }

        ApplyFilter();
    }

    /// <summary>
    /// Xóa vật phẩm khỏi túi đồ (được gọi khi trang bị thành công).
    /// </summary>
    public void RemoveItemFromInventory(ItemData item)
    {
        if (item == null) return;

        var toRemove = allItems.Find(x => x != null && !string.IsNullOrEmpty(x.inventoryId)
                                          && x.inventoryId == item.inventoryId);
        if (toRemove != null)
        {
            allItems.Remove(toRemove);
        }

        ApplyFilter();
    }
}