using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 1. Khai báo kiểu dữ liệu cho Item
public enum ItemType { Weapon, Armor, Accessory, Consumable }

[System.Serializable]
public class Item
{
    public string itemName;
    public ItemType type;
    public Sprite icon;
    public int atkBonus;
    public int defBonus;
}

public class InventoryManager : MonoBehaviour
{
    [Header("--- UI References ---")]
    public Transform gridSlotsContainer; 
    public TMP_Dropdown dropdownFilter;
    public TextMeshProUGUI txtStats;     
    public TextMeshProUGUI txtPageNumber;

    [Header("--- Pagination Buttons ---")]
    public Button btnFirst;
    public Button btnPrev;
    public Button btnNext;
    public Button btnLast;

    [Header("--- Inventory Data ---")]
    public List<Item> allItems = new List<Item>(); // Danh sách toàn bộ item đang có
    private List<Item> filteredItems = new List<Item>(); // Danh sách sau khi lọc

    private int currentPage = 1;
    private int itemsPerPage = 49; // Đúng bằng số ô vuông trên 1 trang của bạn
    private int totalPages = 5;

    void Start()
    {
        // Gán sự kiện cho Dropdown và Nút phân trang
        dropdownFilter.onValueChanged.AddListener(OnFilterChanged);
        btnFirst.onClick.AddListener(() => ChangePage(1));
        btnPrev.onClick.AddListener(() => ChangePage(currentPage - 1));
        btnNext.onClick.AddListener(() => ChangePage(currentPage + 1));
        btnLast.onClick.AddListener(() => ChangePage(totalPages));

        // Tạo dữ liệu giả lập (Dummy Data) để test ngay
        GenerateDummyData();
        ApplyFilter();
    }

    // Tạo dữ liệu test ngẫu nhiên
    void GenerateDummyData()
    {
        allItems.Clear();
        for (int i = 1; i <= 120; i++) // Tạo hẳn 120 item để test phân trang (3 trang)
        {
            ItemType randomType = (ItemType)Random.Range(0, 4);
            allItems.Add(new Item
            {
                itemName = $"{randomType} #{i}",
                type = randomType,
                atkBonus = Random.Range(10, 100),
                defBonus = Random.Range(5, 50)
            });
        }
    }

    void OnFilterChanged(int value)
    {
        currentPage = 1; // Về trang 1 khi đổi bộ lọc
        ApplyFilter();
    }

    void ApplyFilter()
    {
        int filterIndex = dropdownFilter.value; // 0: All, 1: Weapon, 2: Armor, 3: Accessory, 4: Consumable
        filteredItems.Clear();

        if (filterIndex == 0)
        {
            filteredItems.AddRange(allItems);
        }
        else
        {
            ItemType selectedType = (ItemType)(filterIndex - 1);
            filteredItems = allItems.FindAll(item => item.type == selectedType);
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
        // Cập nhật Text trang
        txtPageNumber.text = $"{currentPage} / {totalPages}";

        // Khóa/Mở nút bấm phân trang
        btnFirst.interactable = btnPrev.interactable = (currentPage > 1);
        btnNext.interactable = btnLast.interactable = (currentPage < totalPages);

        // Hiển thị item lên từng ô Slot
        int startIndex = (currentPage - 1) * itemsPerPage;
        int slotCount = gridSlotsContainer.childCount;

        for (int i = 0; i < slotCount; i++)
        {
            Transform slot = gridSlotsContainer.GetChild(i);
            Image slotImage = slot.GetComponent<Image>();

            int itemIndex = startIndex + i;
            if (itemIndex < filteredItems.Count)
            {
                // Có item: Đổi màu ô (hoặc gán sprite) để nhận biết
                slotImage.color = GetColorByItemType(filteredItems[itemIndex].type);
                slot.gameObject.SetActive(true);
            }
            else
            {
                // Ô trống không có item
                slotImage.color = Color.white;
            }
        }

        // Cập nhật chỉ số tổng ngẫu nhiên lên bảng Stats
        UpdateStatsDisplay();
    }

    Color GetColorByItemType(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon: return new Color(1f, 0.4f, 0.4f); // Đỏ nhẹ
            case ItemType.Armor: return new Color(0.4f, 0.6f, 1f);  // Xanh dương
            case ItemType.Accessory: return new Color(1f, 0.9f, 0.3f); // Vàng
            case ItemType.Consumable: return new Color(0.4f, 1f, 0.4f); // Xanh lá
            default: return Color.white;
        }
    }

    void UpdateStatsDisplay()
    {
        if (txtStats != null)
        {
            txtStats.text = $"Tấn công: {Random.Range(5000, 9000)}\n" +
                            $"Phòng thủ: {Random.Range(4000, 8000)}\n" +
                            $"May mắn: {Random.Range(1000, 3000)}";
        }
    }
}