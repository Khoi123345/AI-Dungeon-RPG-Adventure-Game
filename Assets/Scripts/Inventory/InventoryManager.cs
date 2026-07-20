using UnityEngine;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    // Danh sách tất cả các Slots hiển thị trong Grid_Slots_Container
    [SerializeField] private List<InventorySlotUI> allSlots = new List<InventorySlotUI>();

    void Start()
    {
        // Vừa vào game: Hiện đầy đủ tất cả các ô (kể cả ô trống) để đảm bảo thẩm mỹ UI
        ShowAllInventory();
    }

    // ==========================================
    // MODULE 1: LOGIC LỌC VÀ HIỂN THỊ TÚI ĐỒ
    // ==========================================

    public void FilterInventory(string typeString)
    {
        // Chuyển chuỗi chữ truyền từ nút bấm thành kiểu Enum tương ứng
        ItemType selectedType = (ItemType)System.Enum.Parse(typeof(ItemType), typeString);

        foreach (var slot in allSlots)
        {
            // Kiểm tra xem Slot có tồn tại không để tránh lỗi NullReferenceException
            if (slot != null)
            {
                // Kiểm tra xem Slot đó được tích chọn "Has Item" hay không
                if (slot.hasItem) 
                {
                    // So sánh trực tiếp với itemTypeTest trên Slot để phân loại
                    if (slot.itemTypeTest == selectedType)
                    {
                        slot.gameObject.SetActive(true); // Trùng loại -> HIỆN
                    }
                    else 
                    {
                        slot.gameObject.SetActive(false); // Không trùng -> ẨN
                    }
                }
                else
                {
                    // Khi đang bật chế độ Lọc: Ô trống không có đồ thì ẩn đi
                    slot.gameObject.SetActive(false); 
                }
            }
        }
    }

    public void ShowAllInventory()
    {
        foreach (var slot in allSlots)
        {
            if (slot != null)
            {
                slot.gameObject.SetActive(true); // Hiện lại toàn bộ 36 ô đồ
            }
        }
    }
}