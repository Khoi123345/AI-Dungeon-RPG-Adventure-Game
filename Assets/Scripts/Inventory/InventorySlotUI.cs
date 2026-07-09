using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Hỗ trợ bắt sự kiện di chuột (Hover)
using TMPro; // Quản lý chữ TextMeshPro hiển thị số lượng

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Ô này dùng để kiểm tra xem Slot này đang chứa đồ hay đang trống
    public bool hasItem = false;
    public int itemQuantity = 0; // Số lượng vật phẩm hiện tại trong ô

    public ItemData itemData;
    public ItemType itemTypeTest;

    [Header("UI Components")]
    [SerializeField] private Image itemIconImage;          // Icon hiển thị vật phẩm
    [SerializeField] private Image backgroundImage;       // Ảnh nền để thể hiện độ hiếm
    [SerializeField] private TextMeshProUGUI txtQuantity; // Chữ hiển thị số lượng

    [Header("Rarity Colors")]
    [SerializeField] private Color colorCommon = new Color(0.6f, 0.6f, 0.6f, 1f); // Xám
    [SerializeField] private Color colorRare = new Color(0.2f, 0.5f, 0.9f, 1f);   // Xanh dương
    [SerializeField] private Color colorEpic = new Color(0.6f, 0.2f, 0.8f, 1f);   // Tím
    [SerializeField] private Color colorDefault = new Color(0.2f, 0.2f, 0.2f, 0.4f); // Nền trống mặc định

    // Hàm đưa vật phẩm và số lượng vào ô đồ
    public void AddItemToSlot(ItemData newItem, int quantity = 1)
    {
        itemData = newItem;
        hasItem = true;
        itemQuantity = quantity;

        // 1. Cập nhật Icon hiển thị
        if (itemIconImage != null && newItem.itemIcon != null)
        {
            itemIconImage.sprite = newItem.itemIcon;
            itemIconImage.gameObject.SetActive(true);
        }

        // 2. Cập nhật màu nền theo độ hiếm
        UpdateRarityBackground(newItem.itemRarity);

        // 3. Hiển thị số lượng (chỉ hiện text nếu số lượng > 1)
        if (txtQuantity != null)
        {
            if (quantity > 1)
            {
                txtQuantity.text = quantity.ToString();
                txtQuantity.gameObject.SetActive(true);
            }
            else
            {
                txtQuantity.gameObject.SetActive(false);
            }
        }
    }

    // Hàm xóa vật phẩm khỏi ô đồ khi dùng hoặc vứt bỏ
    public void ClearSlot()
    {
        itemData = null;
        hasItem = false;
        itemQuantity = 0;

        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.gameObject.SetActive(false);
        }

        if (txtQuantity != null)
        {
            txtQuantity.gameObject.SetActive(false);
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = colorDefault; // Trả về màu nền trống mặc định
        }
    }

    // Cập nhật màu sắc của background dựa vào Enum độ hiếm
    private void UpdateRarityBackground(ItemRarity rarity)
    {
        if (backgroundImage == null) return;

        switch (rarity)
        {
            case ItemRarity.Common:
                backgroundImage.color = colorCommon;
                break;
            case ItemRarity.Rare:
                backgroundImage.color = colorRare;
                break;
            case ItemRarity.Epic:
                backgroundImage.color = colorEpic;
                break;
            default:
                backgroundImage.color = colorCommon;
                break;
        }
    }

    #region INTERFACE IMPLEMENTATIONS: EVENT SYSTEMS (HỘ TRỢ TOOLTIP HOVER)
    // Kích hoạt khi con chuột di chuyển vào khu vực của Slot này
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hasItem && itemData != null)
        {
            // Hiển thị thông tin tên và loại vật phẩm ra Console (Có thể mở rộng thành popup tooltip UI thực tế)
            Debug.Log($"[Tooltip] {itemData.itemName} - {itemData.itemType} (Độ hiếm: {itemData.itemRarity})");
        }
    }

    // Kích hoạt khi con chuột rời khỏi khu vực của Slot này
    public void OnPointerExit(PointerEventData eventData)
    {
        if (hasItem && itemData != null)
        {
            Debug.Log($"[Tooltip] Đóng Tooltip của {itemData.itemName}");
        }
    }
    #endregion
}