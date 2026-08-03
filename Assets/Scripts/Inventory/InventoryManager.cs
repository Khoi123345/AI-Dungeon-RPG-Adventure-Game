using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    // Danh sách tất cả các Slots hiển thị trong Grid_Slots_Container
    [SerializeField] private List<InventorySlotUI> allSlots = new List<InventorySlotUI>();
    [SerializeField] private List<ItemData> itemDatabase = new List<ItemData>();

    private void OnEnable()
    {
        RefreshInventoryUI();
    }

    private void Start()
    {
        RefreshInventoryUI();
    }

    // ==========================================
    // MODULE 1: LOGIC LỌC VÀ HIỂN THỊ TÚI ĐỒ
    // ==========================================

    public void RefreshInventoryUI()
    {
        // Tự động quét tìm allSlots nếu danh sách rỗng trong Inspector
        if (allSlots == null || allSlots.Count == 0)
        {
            allSlots = new List<InventorySlotUI>(GetComponentsInChildren<InventorySlotUI>(true));
        }

        // Làm sạch tất cả ô
        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                slot.ClearSlot();
                slot.gameObject.SetActive(true); // Giữ ô hiển thị để đẹp lưới UI
            }
        }

        if (GameProgressService.Instance == null) return;
        var inventoryItems = GameProgressService.Instance.GetInventory();
        if (inventoryItems == null || inventoryItems.Count == 0) return;

        int slotIndex = 0;
        foreach (var inv in inventoryItems)
        {
            if (slotIndex >= allSlots.Count) break;

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

            InventorySlotUI slotScript = allSlots[slotIndex];
            slotScript.AddItemToSlot(matchData, inv.quantity);
            slotScript.SetEquipped(inv.equipped);
            slotScript.gameObject.SetActive(true);

            // Gán sự kiện Bấm vào ô để Trang bị / Tháo trang bị theo ID định danh duy nhất (inventoryId)
            string capturedInventoryId = !string.IsNullOrEmpty(inv.inventoryId) ? inv.inventoryId : inv.itemId;
            slotScript.onSlotClicked = (clickedSlot) =>
            {
                if (clickedSlot.hasItem && !string.IsNullOrEmpty(capturedInventoryId))
                {
                    GameProgressService.Instance.ToggleEquipItemByInventoryId(capturedInventoryId);
                    RefreshInventoryUI(); // Cập nhật lại giao diện để hiển thị đúng viền trang bị ô người chơi vừa chọn!
                }
            };

            slotIndex++;
        }

        Debug.Log($"🎒 [INVENTORY UI REFRESH] Đã tải thành công {slotIndex} vật phẩm từ GameProgressService vào giao diện Túi đồ!");
    }

    public void FilterInventory(string typeString)
    {
        if (string.IsNullOrEmpty(typeString) || typeString.Equals("All", System.StringComparison.OrdinalIgnoreCase))
        {
            ShowAllInventory();
            return;
        }

        // Tự động chuẩn hóa tên danh mục (ví dụ: "Consumable Item" -> "Consumable")
        string cleanTypeStr = typeString.Replace(" ", "").Replace("Item", "");

        if (!System.Enum.TryParse<ItemType>(cleanTypeStr, true, out ItemType selectedType))
        {
            Debug.LogWarning($"[InventoryManager] Không thể nhận diện danh mục filter '{typeString}'.");
            return;
        }

        int activeCount = 0;
        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                // Kiểm tra chính xác 100% itemData.itemType của vật phẩm trong ô
                if (slot.hasItem && slot.itemData != null && slot.itemData.itemType == selectedType)
                {
                    slot.gameObject.SetActive(true);
                    activeCount++;
                }
                else
                {
                    slot.gameObject.SetActive(false); 
                }
            }
        }

        Debug.Log($"🎒 [INVENTORY FILTER] Đã lọc theo danh mục '{selectedType}': Hiển thị {activeCount} vật phẩm hợp lệ.");
    }

    public void ShowAllInventory()
    {
        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                slot.gameObject.SetActive(true);
            }
        }
    }
}