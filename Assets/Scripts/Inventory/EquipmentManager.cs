using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton quản lý toàn bộ logic Trang bị / Gỡ trang bị.
/// Kết nối túi đồ bên phải (InventorySlotUI) với ô trang bị bên trái (EquipmentSlotUI).
///
/// SETUP TRONG UNITY:
/// 1. Tạo một GameObject tên "EquipmentManager" trong Scene (hoặc gán vào Panel_Inventory).
/// 2. Gán script này vào GameObject đó.
/// 3. Kéo các EquipmentSlotUI bên trái vào mảng equipmentSlots.
/// 4. Tạo popup UI với 2 nút "Trang bị" và "Gỡ" rồi kéo vào Inspector.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    // ──────────────────────────────────────────────
    //  Inspector References
    // ──────────────────────────────────────────────

    [Header("Ô trang bị bên trái (Left_CharacterPanel)")]
    [Tooltip("Kéo tất cả CharEquipSlotUI của Left_CharacterPanel vào đây")]
    public List<CharEquipSlotUI> equipmentSlots = new List<CharEquipSlotUI>();

    [Header("Popup Action (hiện khi click slot)")]
    [Tooltip("Panel popup chứa 2 nút Trang Bị / Go")]
    [SerializeField] private GameObject actionPopup;
    [SerializeField] private Button btnEquip;    // Nút 'Trang bị' (cho inventory slot bên phải)
    [SerializeField] private Button btnUnequip;  // Nút 'Go' (cho equipment slot bên trái)
    [SerializeField] private TextMeshProUGUI txtPopupItemName; // Tên item trên popup

    [Header("InventoryManager (tham chiếu)")]
    [SerializeField] private InventoryManager inventoryManager;

    // ──────────────────────────────────────────────
    //  State
    // ──────────────────────────────────────────────
    private InventorySlotUI  selectedInventorySlot;
    private CharEquipSlotUI  selectedEquipmentSlot;

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Gán sự kiện cho 2 nút
        if (btnEquip   != null) btnEquip.onClick.AddListener(OnClickEquip);
        if (btnUnequip != null) btnUnequip.onClick.AddListener(OnClickUnequip);

        HidePopup();
    }

    void Update()
    {
        // Đóng context menu khi người chơi click TRÁI vào bất kỳ đâu bên ngoài
        if (actionPopup != null && actionPopup.activeSelf)
        {
            if (Input.GetMouseButtonDown(0)) // 0 = chuột trái
            {
                HidePopup();
            }
        }
    }

    // ──────────────────────────────────────────────
    //  Được gọi từ InventorySlotUI khi click slot bên phải
    // ──────────────────────────────────────────────
    public void OnInventorySlotClicked(InventorySlotUI slot)
    {
        if (!slot.hasItem || slot.itemData == null) return;

        selectedInventorySlot = slot;
        selectedEquipmentSlot = null;

        // Popup chỉ hiện nút "Trang bị", ẩn nút "Go"
        if (btnEquip   != null) btnEquip.gameObject.SetActive(true);
        if (btnUnequip != null) btnUnequip.gameObject.SetActive(false);

        if (txtPopupItemName != null)
            txtPopupItemName.text = slot.itemData.itemName;

        ShowPopupNearMouse();
    }

    // ──────────────────────────────────────────────
    //  Được gọi từ EquipmentSlotUI khi click slot bên trái
    // ──────────────────────────────────────────────
    public void OnEquipSlotClicked(CharEquipSlotUI slot)
    {
        if (!slot.isEquipped) return;

        selectedEquipmentSlot = slot;
        selectedInventorySlot = null;

        // Popup chỉ hiện nút "Go", ẩn nút "Trang bi"
        if (btnEquip   != null) btnEquip.gameObject.SetActive(false);
        if (btnUnequip != null) btnUnequip.gameObject.SetActive(true);

        if (txtPopupItemName != null)
            txtPopupItemName.text = slot.equippedItem.itemName;

        ShowPopupNearMouse();
    }

    // ──────────────────────────────────────────────
    //  Logic Trang bị
    // ──────────────────────────────────────────────
    private void OnClickEquip()
    {
        if (selectedInventorySlot == null || selectedInventorySlot.itemData == null) return;

        ItemData item = selectedInventorySlot.itemData;

        // Tìm ô trang bị phù hợp với loại item
        CharEquipSlotUI targetSlot = FindEquipmentSlot(item.itemType);
        if (targetSlot == null)
        {
            Debug.LogWarning($"[EquipmentManager] Không tìm thấy ô trang bị cho loại: {item.itemType}");
            HidePopup();
            return;
        }

        // Nếu ô trang bị đang có item khác → trả item đó về túi đồ trước
        if (targetSlot.isEquipped && inventoryManager != null)
        {
            inventoryManager.AddItemToInventory(targetSlot.equippedItem);
            Debug.Log($"[EquipmentManager] Hoán đổi: {targetSlot.equippedItem.itemName} trả về túi đồ.");
        }

        // Trang bị item vào ô trái
        targetSlot.EquipItem(item);

        // Xóa item khỏi túi đồ bên phải
        if (inventoryManager != null)
            inventoryManager.RemoveItemFromInventory(item);

        Debug.Log($"[EquipmentManager] Da trang bi: {item.itemName}");

        HidePopup();
        UpdateStatsDisplay();
    }

    // ──────────────────────────────────────────────
    //  Logic Gỡ trang bị
    // ──────────────────────────────────────────────
    private void OnClickUnequip()
    {
        if (selectedEquipmentSlot == null || !selectedEquipmentSlot.isEquipped) return;

        // Gỡ item ra
        ItemData removedItem = selectedEquipmentSlot.UnequipItem();

        // Trả item về túi đồ bên phải
        if (inventoryManager != null)
            inventoryManager.AddItemToInventory(removedItem);

        Debug.Log($"[EquipmentManager] Da go trang bi: {removedItem.itemName}");

        HidePopup();
        UpdateStatsDisplay();
    }

    // ──────────────────────────────────────────────
    //  Tính tổng chỉ số từ các ô trang bị đang đeo
    // ──────────────────────────────────────────────
    public void UpdateStatsDisplay()
    {
        if (inventoryManager == null) return;

        int totalAtk = 0, totalDef = 0;
        foreach (var slot in equipmentSlots)
        {
            if (slot.isEquipped)
            {
                totalAtk += slot.equippedItem.atkBonus;
                totalDef += slot.equippedItem.defBonus;
            }
        }

        if (inventoryManager.txtAtk != null)
            inventoryManager.txtAtk.text = $"Tan cong: {totalAtk}";
        if (inventoryManager.txtDef != null)
            inventoryManager.txtDef.text = $"Phong thu: {totalDef}";
    }

    // ──────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────
    private CharEquipSlotUI FindEquipmentSlot(ItemType type)
    {
        foreach (var slot in equipmentSlots)
            if (slot.equipSlotType == type) return slot;
        return null;
    }

    private void ShowPopupNearMouse()
    {
        if (actionPopup == null) return;
        actionPopup.SetActive(true);

        // Đặt popup gần vị trí chuột
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.mousePosition, canvas.worldCamera, out Vector2 localPos);
            RectTransform popupRect = actionPopup.GetComponent<RectTransform>();
            if (popupRect != null)
                popupRect.anchoredPosition = localPos + new Vector2(10f, -10f);
        }
    }

    public void HidePopup()
    {
        if (actionPopup != null) actionPopup.SetActive(false);
        selectedInventorySlot = null;
        selectedEquipmentSlot = null;
    }
}
