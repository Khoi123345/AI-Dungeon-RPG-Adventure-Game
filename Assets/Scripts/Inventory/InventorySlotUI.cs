using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Hỗ trợ bắt sự kiện di chuột (Hover)
using TMPro; // Quản lý chữ TextMeshPro hiển thị số lượng

public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    // Sự kiện callback khi người chơi click/chọn ô này
    public System.Action<InventorySlotUI> onSlotClicked;

    // Ô này dùng để kiểm tra xem Slot này đang chứa đồ hay đang trống
    public bool hasItem = false;
    public int itemQuantity = 0; // Số lượng vật phẩm hiện tại trong ô
    public bool isEquipped { get; private set; } = false;

    public ItemData itemData;
    public ItemType itemTypeTest;

    public void SetEquipped(bool equipped)
    {
        isEquipped = equipped;
        transform.localScale = Vector3.one;
        if (hasItem && itemData != null)
        {
            UpdateRarityBackground(itemData.itemRarity);
        }
        else if (backgroundImage != null)
        {
            backgroundImage.color = colorDefault;
        }
    }

    [Header("UI Components")]
    [SerializeField] private Image itemIconImage;          // Icon hiển thị vật phẩm
    [SerializeField] private Image backgroundImage;       // Ảnh nền để thể hiện độ hiếm
    [SerializeField] private TextMeshProUGUI txtQuantity; // Chữ hiển thị số lượng

    [Header("Rarity Colors")]
    [SerializeField] private Color colorCommon = new Color(0.6f, 0.6f, 0.6f, 1f); // Xám
    [SerializeField] private Color colorRare = new Color(0.2f, 0.5f, 0.9f, 1f);   // Xanh dương
    [SerializeField] private Color colorEpic = new Color(0.6f, 0.2f, 0.8f, 1f);   // Tím
    [SerializeField] private Color colorDefault = Color.white; // Nền ô màu trắng mặc định

    private void AutoFindComponentsIfNeeded()
    {
        if (itemIconImage == null)
        {
            Transform iconTr = transform.Find("Item icon") ?? transform.Find("item icon") ?? transform.Find("Icon");
            if (iconTr != null) itemIconImage = iconTr.GetComponent<Image>();
            if (itemIconImage == null)
            {
                Image[] imgs = GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img.gameObject != this.gameObject) { itemIconImage = img; break; }
                }
            }
        }

        if (txtQuantity == null)
        {
            Transform txtTr = transform.Find("txt_Quantity") ?? transform.Find("txtQuantity") ?? transform.Find("Quantity");
            if (txtTr != null) txtQuantity = txtTr.GetComponent<TextMeshProUGUI>();
            if (txtQuantity == null) txtQuantity = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }
    }

    // Hàm đưa vật phẩm và số lượng vào ô đồ
    public void AddItemToSlot(ItemData newItem, int quantity = 1)
    {
        AutoFindComponentsIfNeeded();

        itemData = newItem;
        hasItem = true;
        itemQuantity = quantity;

        // 1. Cập nhật Icon hiển thị
        if (itemIconImage != null)
        {
            if (newItem != null && newItem.itemIcon != null)
            {
                itemIconImage.sprite = newItem.itemIcon;
                itemIconImage.color = Color.white;
                itemIconImage.gameObject.SetActive(true);
            }
            else
            {
                itemIconImage.gameObject.SetActive(false);
            }
        }

        // 2. Cập nhật màu nền theo độ hiếm
        if (newItem != null)
        {
            UpdateRarityBackground(newItem.itemRarity);
        }

        // 3. Hiển thị số lượng hoặc Tên (nếu thiếu Icon)
        if (txtQuantity != null)
        {
            if (quantity > 1)
            {
                txtQuantity.text = quantity.ToString();
                txtQuantity.gameObject.SetActive(true);
            }
            else if (newItem != null && newItem.itemIcon == null && !string.IsNullOrEmpty(newItem.itemName))
            {
                txtQuantity.text = newItem.itemName;
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

    [Header("Selection Highlight")]
    [SerializeField] private Image selectionHighlightImage; // Viền sáng Highlight
    [SerializeField] private Color colorSelected = new Color(1f, 0.85f, 0.2f, 1f); // Màu Vàng Sáng khi được chọn
    public bool isSelected { get; private set; } = false;

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        // 1. Hiệu ứng phóng to nhẹ (Pop-out) khi được chọn
        transform.localScale = selected ? new Vector3(1.12f, 1.12f, 1f) : Vector3.one;

        // 2. Cập nhật màu sắc Highlight
        if (selectionHighlightImage != null)
        {
            selectionHighlightImage.gameObject.SetActive(selected);
        }
        else if (backgroundImage != null)
        {
            if (selected)
            {
                backgroundImage.color = colorSelected;
            }
            else if (hasItem && itemData != null)
            {
                UpdateRarityBackground(itemData.itemRarity);
            }
            else
            {
                backgroundImage.color = colorDefault;
            }
        }

        if (selected && hasItem && itemData != null)
        {
            Debug.Log($"✨ [SLOT HIGHLIGHTED] Ô vật phẩm '{itemData.itemName}' đã được ĐÁNH DẤU CHỌN (SELECTED)!");
        }
    }

    // Cập nhật màu sắc của background dựa vào Enum độ hiếm
    private void UpdateRarityBackground(ItemRarity rarity)
    {
        if (backgroundImage == null) return;
        if (isSelected)
        {
            backgroundImage.color = colorSelected;
            return;
        }

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

    #region INTERFACE IMPLEMENTATIONS: EVENT SYSTEMS (TOOLTIP + CLICK)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hasItem && itemData != null && ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.ShowTooltip(itemData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.HideTooltip();
    }

    // Xử lý click: chuột PHẢI → context menu "Trang bị", chuột TRÁI → fire callback
    public void OnPointerClick(PointerEventData eventData)
    {
        // Chuột PHẢI → mở context menu trang bị
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (!hasItem || itemData == null) return;

            if (EquipmentManager.Instance != null)
                EquipmentManager.Instance.OnInventorySlotClicked(this);
            return;
        }

        // Chuột TRÁI → log thông tin và fire callback
        if (hasItem && itemData != null)
        {
            Debug.Log($"🎯 [ITEM CLICKED LOG] Bạn đã BẤM CHỌN vật phẩm: '{itemData.itemName}' | Phẩm chất: {itemData.itemRarity} | Loại: {itemData.itemType} x{itemQuantity}");
        }
        else
        {
            Debug.Log($"🎯 [SLOT CLICKED LOG] Bạn đã chọn vào một Ô trống.");
        }

        onSlotClicked?.Invoke(this);
    }
    #endregion
}
