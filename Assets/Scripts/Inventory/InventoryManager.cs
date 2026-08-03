using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// InventoryGridItem đã bị loại bỏ — thay bằng ItemData (xem ItemData.cs)
// ItemData giờ là [System.Serializable] nên có thể "new" trực tiếp trong code

public class InventoryManager : MonoBehaviour
{
    [Header("--- UI References ---")]
    public Transform gridSlotsContainer; 
    public TMP_Dropdown dropdownFilter;
    public TextMeshProUGUI txtAtk;       // Text (TMP) (1) — Tấn công
    public TextMeshProUGUI txtDef;       // Text (TMP) (2) — Phòng thủ
    public TextMeshProUGUI txtPageNumber;

    [Header("--- Pagination Buttons ---")]
    public Button btnFirst;
    public Button btnPrev;
    public Button btnNext;
    public Button btnLast;

    [Header("--- Inventory Data ---")]
    public List<ItemData> allItems = new List<ItemData>(); // Danh sách toàn bộ item đang có
    private List<ItemData> filteredItems = new List<ItemData>(); // Danh sách sau khi lọc

    private int currentPage = 1;
    private int itemsPerPage = 49; // Đúng bằng số ô vuông trên 1 trang của bạn
    private int totalPages = 1;

    void Start()
    {
        // Gán sự kiện cho Dropdown và Nút phân trang
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

        // Nếu đã có dữ liệu thật (load từ API), render luôn
        if (gridSlotsContainer != null)
        {
            ApplyFilter();
        }
        else
        {
            Debug.LogWarning("[InventoryManager] Chưa gán 'Grid Slots Container' trong Inspector.");
        }
    }

    /// <summary>
    /// Gọi hàm này để nạp dữ liệu thật từ API vào túi đồ.
    /// Ví dụ: InventoryManager.Instance.LoadItems(responseData);
    /// </summary>
    public void LoadItems(List<ItemData> items)
    {
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[InventoryManager] Dữ liệu item trống, không có gì để hiển thị.");
            return;
        }

        allItems = items;
        currentPage = 1;
        ApplyFilter();

        Debug.Log($"[InventoryManager] Đã nạp {allItems.Count} item vào túi đồ.");
    }

    void OnFilterChanged(int value)
    {
        currentPage = 1; // Về trang 1 khi đổi bộ lọc
        ApplyFilter();
    }

    void ApplyFilter()
    {
        // Kiểm tra null để tránh lỗi khi dropdownFilter chưa được gán
        if (dropdownFilter == null) return;

        int filterIndex = dropdownFilter.value; // 0: All, 1: Weapon, 2: Armor, 3: Accessory, 4: Consumable
        filteredItems.Clear();

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

        // --- Hiển thị item lên từng ô Slot ---
        int startIndex = (currentPage - 1) * itemsPerPage;
        int slotCount  = gridSlotsContainer.childCount;

        for (int i = 0; i < slotCount; i++)
        {
            Transform slot = gridSlotsContainer.GetChild(i);

            // Lấy component InventorySlotUI trên slot đó
            InventorySlotUI slotUI = slot.GetComponent<InventorySlotUI>();

            int itemIndex = startIndex + i;

            if (itemIndex < filteredItems.Count)
            {
                ItemData item = filteredItems[itemIndex];

                if (slotUI != null)
                {
                    // ✅ Gọi đúng hàm — hiển thị icon, độ hiếm, số lượng
                    slotUI.AddItemToSlot(item, item.quantity);
                }
                else
                {
                    // Fallback: nếu slot không có InventorySlotUI thì chỉ đổi màu đơn giản
                    Image slotImage = slot.GetComponent<Image>();
                    if (slotImage != null)
                        slotImage.color = GetColorByItemType(item.itemType);
                }

                slot.gameObject.SetActive(true);
            }
            else
            {
                // Ô trống — xóa hiển thị
                if (slotUI != null)
                    slotUI.ClearSlot();
                else
                {
                    Image slotImage = slot.GetComponent<Image>();
                    if (slotImage != null)
                        slotImage.color = Color.white;
                }
            }
        }

        // --- Cập nhật bảng Stats ---
        UpdateStatsDisplay();
    }

    // Fallback color — chỉ dùng khi slot không có InventorySlotUI
    Color GetColorByItemType(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon:    return new Color(1f, 0.4f, 0.4f);  // Đỏ nhẹ
            case ItemType.Armor:     return new Color(0.4f, 0.6f, 1f);  // Xanh dương
            case ItemType.Accessory: return new Color(1f, 0.9f, 0.3f);  // Vàng
            case ItemType.Consumable:return new Color(0.4f, 1f, 0.4f);  // Xanh lá
            default:                 return Color.white;
        }
    }

    // Tính tổng ATK + DEF từ toàn bộ item trong túi rồi hiển thị
    void UpdateStatsDisplay()
    {
        int totalAtk = 0;
        int totalDef = 0;

        foreach (ItemData item in allItems)
        {
            totalAtk += item.atkBonus;
            totalDef += item.defBonus;
        }

        if (txtAtk != null) txtAtk.text = $"Tấn công: {totalAtk}";
        if (txtDef != null) txtDef.text = $"Phòng thủ: {totalDef}";
    }
}