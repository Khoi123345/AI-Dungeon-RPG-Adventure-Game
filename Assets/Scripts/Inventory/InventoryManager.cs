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

    [Header("--- Test / Debug ---")]
    [Tooltip("Tích vào để tự động tạo dữ liệu giả khi Play (dùng để test tooltip)")]
    public bool useDummyData = true;
    [Tooltip("Số lượng item giả muốn tạo")]
    public int dummyItemCount = 20;

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

        // Tạo dữ liệu giả để test nếu được bật và chưa có dữ liệu thật
        if (useDummyData && allItems.Count == 0)
        {
            GenerateDummyData(dummyItemCount);
            Debug.Log($"[InventoryManager] Đã tạo {dummyItemCount} item giả để test.");
        }

        // Render lên UI
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
    /// Tạo danh sách item ngẫu nhiên để test UI — KHÔNG dùng trong production.
    /// </summary>
    public void GenerateDummyData(int count = 20)
    {
        allItems.Clear();

        // Tên mẫu theo từng loại
        string[] weaponNames  = { "Kiếm Lửa", "Đại Kiếm Bóng Tối", "Cung Gió", "Trượng Phù Thủy", "Dao Găm Máu" };
        string[] armorNames   = { "Giáp Rồng", "Áo Choàng Bóng", "Khiên Thần Thánh", "Giáp Sắt", "Áo Giáp Da" };
        string[] accessNames  = { "Nhẫn Lửa", "Vòng Cổ Tinh Tú", "Bùa Hộ Mệnh", "Huy Hiệu Dũng Sĩ", "Khuyên Tai Bí Ẩn" };
        string[] consumeNames = { "Bình Máu", "Bình Phép", "Thuốc Tăng Lực", "Cuộn Hồi Sinh", "Đá Mài Kiếm" };

        ItemType[]   types    = { ItemType.Weapon, ItemType.Armor, ItemType.Accessory, ItemType.Consumable };
        ItemRarity[] rarities = { ItemRarity.Common, ItemRarity.Rare, ItemRarity.Epic };
        string[][]   namePool = { weaponNames, armorNames, accessNames, consumeNames };

        for (int i = 0; i < count; i++)
        {
            int typeIdx   = i % types.Length;  // Xoay vòng đều 4 loại
            ItemType   t  = types[typeIdx];
            ItemRarity r  = rarities[Random.Range(0, rarities.Length)];

            string[] pool = namePool[typeIdx];
            string name   = pool[Random.Range(0, pool.Length)];

            // Thêm số thứ tự để tên không bị trùng
            if (count > pool.Length)
                name += $" +{i / types.Length}";

            ItemData item = new ItemData
            {
                itemName    = name,
                itemType    = t,
                itemRarity  = r,
                itemIcon    = null, // Không có icon — ô sẽ hiển thị trắng, tooltip vẫn hoạt động
                atkBonus    = (t == ItemType.Weapon)    ? Random.Range(5, 50)  : Random.Range(0, 10),
                defBonus    = (t == ItemType.Armor)     ? Random.Range(5, 40)  : Random.Range(0, 8),
                quantity    = (t == ItemType.Consumable) ? Random.Range(1, 10) : 1
            };

            allItems.Add(item);
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

    /// <summary>Thêm item về túi đồ (dùng khi gỡ trang bị).</summary>
    public void AddItemToInventory(ItemData item)
    {
        if (item == null) return;
        allItems.Add(item);
        ApplyFilter();
        Debug.Log($"[InventoryManager] Đã thêm {item.itemName} về túi đồ.");
    }

    /// <summary>Xóa item khỏi túi đồ (dùng khi trang bị).</summary>
    public void RemoveItemFromInventory(ItemData item)
    {
        if (item == null) return;
        allItems.Remove(item);
        ApplyFilter();
        Debug.Log($"[InventoryManager] Đã xóa {item.itemName} khỏi túi đồ.");
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