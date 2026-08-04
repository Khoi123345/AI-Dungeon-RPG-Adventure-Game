using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gán vào từng ô trang bị bên trái (Left_CharacterPanel).
/// Mỗi ô đại diện cho 1 vị trí trang bị (Weapon, Armor, Accessory...).
///
/// SETUP TRONG UNITY:
/// 1. Gán script này vào từng ô trang bị bên trái.
/// 2. Kéo Image icon, Image background, txtSlotLabel vào Inspector.
/// 3. Đặt đúng equipSlotType cho từng ô (Weapon, Armor, Accessory...).
/// </summary>
public class CharEquipSlotUI : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    [Header("Loại vị trí trang bị của ô này")]
    public ItemType equipSlotType; // Ô này nhận loại item nào (Weapon, Armor, Accessory...)

    [Header("UI Components")]
    [SerializeField] private Image itemIconImage;      // Icon hiển thị item đang trang bị
    [SerializeField] private Image backgroundImage;    // Nền ô (đổi màu theo độ hiếm)
    [SerializeField] private TextMeshProUGUI txtSlotLabel; // Nhãn ô trống (VD: "Vũ khí")

    [Header("Màu nền theo độ hiếm")]
    [SerializeField] private Color colorCommon  = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color colorRare    = new Color(0.2f, 0.5f, 0.9f, 1f);
    [SerializeField] private Color colorEpic    = new Color(0.6f, 0.2f, 0.8f, 1f);
    [SerializeField] private Color colorEmpty   = new Color(0.2f, 0.2f, 0.2f, 0.4f);

    // Item đang được trang bị vào ô này (null = trống)
    public ItemData equippedItem { get; private set; }
    public bool isEquipped => equippedItem != null;

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    /// <summary>Trang bị item vào ô này.</summary>
    public void EquipItem(ItemData item)
    {
        equippedItem = item;

        // Hiện icon
        if (itemIconImage != null)
        {
            if (item.itemIcon != null)
            {
                itemIconImage.sprite = item.itemIcon;
                itemIconImage.color  = Color.white;
            }
            itemIconImage.gameObject.SetActive(true);
        }

        // Ẩn nhãn trống
        if (txtSlotLabel != null)
            txtSlotLabel.gameObject.SetActive(false);

        // Đổi màu nền theo độ hiếm
        UpdateBackground(item.itemRarity);
    }

    /// <summary>Gỡ item khỏi ô, trả về item vừa gỡ.</summary>
    public ItemData UnequipItem()
    {
        ItemData removed = equippedItem;
        equippedItem = null;

        // Ẩn icon
        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.gameObject.SetActive(false);
        }

        // Hiện lại nhãn trống
        if (txtSlotLabel != null)
            txtSlotLabel.gameObject.SetActive(true);

        // Trả về màu nền trống
        if (backgroundImage != null)
            backgroundImage.color = colorEmpty;

        return removed;
    }

    // ──────────────────────────────────────────────
    //  Click handler — mở popup "Gỡ trang bị"
    // ──────────────────────────────────────────────
    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (!isEquipped) return;

        // Chỉ xử lý khi click chuột PHẢI
        if (eventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Right) return;

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.OnEquipSlotClicked(this);
    }

    // ──────────────────────────────────────────────
    //  Private
    // ──────────────────────────────────────────────
    private void UpdateBackground(ItemRarity rarity)
    {
        if (backgroundImage == null) return;
        switch (rarity)
        {
            case ItemRarity.Common: backgroundImage.color = colorCommon; break;
            case ItemRarity.Rare:   backgroundImage.color = colorRare;   break;
            case ItemRarity.Epic:   backgroundImage.color = colorEpic;   break;
            default:                backgroundImage.color = colorCommon; break;
        }
    }
}
