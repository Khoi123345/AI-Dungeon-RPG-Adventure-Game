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

    [Header("--- Pagination Buttons ---")]
    public Button btnFirst;
    public Button btnPrev;
    public Button btnNext;
    public Button btnLast;

    [Header("--- Templates & Database ---")]
    [SerializeField] private List<ItemData> itemDatabase = new List<ItemData>();

    [Header("--- Inventory Data ---")]
    public List<ItemData> allItems = new List<ItemData>(); // Danh sách toàn bộ item đang có
    private List<ItemData> filteredItems = new List<ItemData>(); // Danh sách sau khi lọc

    private int currentPage = 1;
    private int itemsPerPage = 49; // Đúng bằng số ô vuông trên 1 trang
    private int totalPages = 1;

    void Start()
    {
        // Gán sự kiện cho Dropdown, Nút phân trang và Nút Đóng UI
        if (dropdownFilter != null)
            dropdownFilter.onValueChanged.AddListener(OnFilterChanged);

        if (btnFirst != null)
            btnFirst.onClick.AddListener(() => ChangePage(1));
        if (btnPrev != null)
            btnPrev.onClick.AddListener(() => ChangePage(currentPage - 1));
        if (btnNext != null)
            btnNext.onClick.AddListener(() => ChangePage(currentPage + 1));
        if (btnLast != null)
            btnLast.onClick.AddListener(() => ChangePage(totalPages));
        if (btnClose != null)
            btnClose.onClick.AddListener(() => gameObject.SetActive(false));

        RefreshInventoryUI();
    }

    private void OnEnable()
    {
        RefreshInventoryUI();
    }

    public void RefreshInventoryUI()
    {
        LoadInventoryFromProgressService();
        ApplyFilter();
    }

    private void LoadInventoryFromProgressService()
    {
        allItems.Clear();
        if (GameProgressService.Instance == null) return;
        var inventoryItems = GameProgressService.Instance.GetInventory();
        if (inventoryItems == null || inventoryItems.Count == 0) return;

        foreach (var inv in inventoryItems)
        {
            ItemData matchData = null;
            if (itemDatabase != null)
            {
                matchData = itemDatabase.Find(x => x != null && 
                    !string.IsNullOrEmpty(x.itemName) &&
                    x.itemName.Equals(inv.itemId, System.StringComparison.OrdinalIgnoreCase));
            }

            if (matchData == null)
            {
                matchData = new ItemData
                {
                    itemName = string.IsNullOrEmpty(inv.itemId) ? "Inventory Item" : inv.itemId,
                    itemType = ItemData.GetItemTypeFromId(inv.itemId)
                };
            }
            else
            {
                matchData.itemType = ItemData.GetItemTypeFromId(inv.itemId);
            }

            matchData.quantity = inv.quantity;
            matchData.inventoryId = !string.IsNullOrEmpty(inv.inventoryId) ? inv.inventoryId : inv.itemId;
            matchData.isEquipped = inv.equipped;

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

        if (filterIndex == 0)
        {
            filteredItems.AddRange(allItems);
        }
        else
        {
            ItemType selectedType = (ItemType)(filterIndex - 1);
            filteredItems = allItems.FindAll(item => item.itemType == selectedType);
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

        // --- Hiển thị item lên từng ô Slot ---
        int startIndex = (currentPage - 1) * itemsPerPage;
        int slotCount  = gridSlotsContainer.childCount;

        for (int i = 0; i < slotCount; i++)
        {
            Transform slot = gridSlotsContainer.GetChild(i);
            InventorySlotUI slotUI = slot.GetComponent<InventorySlotUI>();

            int itemIndex = startIndex + i;

            if (itemIndex < filteredItems.Count)
            {
                ItemData item = filteredItems[itemIndex];

                if (slotUI != null)
                {
                    slotUI.AddItemToSlot(item, item.quantity);
                    slotUI.SetEquipped(item.isEquipped);

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
                slot.gameObject.SetActive(true);
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
}