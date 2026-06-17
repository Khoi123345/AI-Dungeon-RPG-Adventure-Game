using UnityEngine;
using UnityEngine.UI; // Quản lý hình ảnh hiển thị vật phẩm

public class InventorySlotUI : MonoBehaviour
{
    // Ô này dùng để kiểm tra xem Slot này đang chứa đồ hay đang trống
    public bool hasItem = false;

    public ItemData itemData;
    public ItemType itemTypeTest;

    // Thành phần UI Image trên Slot để hiển thị hình ảnh của vật phẩm
    [SerializeField] private Image itemIconImage;

    // Hàm này dùng để đưa vật phẩm vào ô đồ (sau này bạn nhặt đồ sẽ cần)
    public void AddItemToSlot(ItemData newItem)
    {
        itemData = newItem;
        hasItem = true;

        if (itemIconImage != null && newItem.itemIcon != null)
        {
            itemIconImage.sprite = newItem.itemIcon;
            itemIconImage.gameObject.SetActive(true); // Hiện hình ảnh lên
        }
    }

    // Hàm này dùng để xóa vật phẩm khỏi ô đồ khi dùng hoặc vứt bỏ
    public void ClearSlot()
    {
        itemData = null;
        hasItem = false;

        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.gameObject.SetActive(false); // Ẩn hình ảnh đi
        }
    }
}