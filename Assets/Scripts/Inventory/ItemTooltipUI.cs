using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton quản lý panel Tooltip hiển thị thông tin vật phẩm khi hover.
/// 
/// CÁCH SETUP TRONG UNITY:
/// 1. Tạo một GameObject con bên trong Canvas, đặt tên "ItemTooltip".
/// 2. Gán script này vào GameObject đó.
/// 3. Tạo cấu trúc UI con bên trong "ItemTooltip":
///    - Panel (Image background, màu tối bán trong suốt)
///    - txtItemName  (TextMeshProUGUI) — Tên vật phẩm
///    - txtItemType  (TextMeshProUGUI) — Loại / Độ hiếm
///    - txtItemStats (TextMeshProUGUI) — Chỉ số ATK / DEF
///    - txtItemDesc  (TextMeshProUGUI) — Mô tả (tùy chọn)
/// 4. Gán các Text đó vào các trường tương ứng trong Inspector.
/// 5. Đảm bảo Raycast Target của Panel ĐƯỢC TẮT để không chặn chuột sang slot khác.
/// 6. Tooltip Panel phải nằm TRÊN cùng trong Hierarchy (Sort Order cao hơn).
/// </summary>
public class ItemTooltipUI : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  Singleton
    // ──────────────────────────────────────────────
    public static ItemTooltipUI Instance { get; private set; }

    // ──────────────────────────────────────────────
    //  Inspector References
    // ──────────────────────────────────────────────
    [Header("Panel gốc của Tooltip (sẽ bị Ẩn/Hiện)")]
    [SerializeField] private GameObject tooltipPanel;

    [Header("Các Text bên trong Tooltip")]
    [SerializeField] private TextMeshProUGUI txtItemName;   // Tên vật phẩm
    [SerializeField] private TextMeshProUGUI txtItemType;   // Loại & Độ hiếm
    [SerializeField] private TextMeshProUGUI txtItemStats;  // ATK / DEF
    [SerializeField] private TextMeshProUGUI txtItemDesc;   // Mô tả (có thể để trống)

    [Header("Tùy chỉnh vị trí")]
    [Tooltip("Tích vào để tooltip hiện ở vị trí cố định thay vì theo chuột")]
    [SerializeField] private bool useFixedPosition = false;

    [Tooltip("Kéo một RectTransform vào đây làm điểm neo cố định cho tooltip (chỉ dùng khi useFixedPosition = true)")]
    [SerializeField] private RectTransform fixedAnchor;

    [Tooltip("Khoảng cách lệch so với con trỏ chuột — chỉ dùng khi useFixedPosition = false (pixel)")]
    [SerializeField] private Vector2 offset = new Vector2(15f, -15f);

    // RectTransform của tooltipPanel để tính toán vị trí
    private RectTransform tooltipRect;
    // Canvas mà tooltip thuộc về (dùng để convert tọa độ)
    private Canvas parentCanvas;
    // Coroutine để delay ẩn tooltip (tránh flicker)
    private Coroutine hideCoroutine;

    // ──────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────
    void Awake()
    {
        // Thiết lập Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Cache các component
        if (tooltipPanel != null)
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();

        parentCanvas = GetComponentInParent<Canvas>();

        // Tự động tắt Raycast Target trên toàn bộ tooltip (panel + text)
        // Đây là nguyên nhân gây ra flicker: tooltip che chuột → slot mất hover
        DisableAllRaycastTargets();

        // Ẩn tooltip lúc đầu
        HideTooltip();
    }

    void Update()
    {
        // Chỉ theo chuột nếu KHÔNG dùng vị trí cố định
        if (!useFixedPosition && tooltipPanel != null && tooltipPanel.activeSelf)
        {
            MoveTooltipToMouse();
        }
    }

    // ──────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Hiện tooltip với thông tin của <paramref name="item"/>.
    /// Gọi hàm này từ InventorySlotUI.OnPointerEnter()
    /// </summary>
    public void ShowTooltip(ItemData item)
    {
        if (item == null || tooltipPanel == null) return;

        // Hủy pending hide nếu đang chờ ẩn — ngăn flicker khi di chuột nhanh giữa các slot
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        // -- Tên --
        if (txtItemName != null)
        {
            txtItemName.text = item.itemName;
            txtItemName.color = GetRarityColor(item.itemRarity);
        }

        // -- Loại & Độ hiếm --
        if (txtItemType != null)
        {
            string typeName  = GetTypeName(item.itemType);
            string rarityName = GetRarityName(item.itemRarity);
            txtItemType.text = $"{typeName}  •  {rarityName}";
            txtItemType.color = GetRarityColor(item.itemRarity);
        }

        // -- Chỉ số --
        if (txtItemStats != null)
        {
            string stats = "";
            if (item.atkBonus != 0) stats += $"[ATK]  Tan cong: <color=#FF6B6B>+{item.atkBonus}</color>\n";
            if (item.defBonus != 0) stats += $"[DEF]  Phong thu: <color=#74B9FF>+{item.defBonus}</color>\n";
            if (stats == "") stats = "<color=#aaaaaa>Khong co chi so chien dau</color>";
            txtItemStats.text = stats.TrimEnd('\n');
        }

        // -- Mô tả --
        if (txtItemDesc != null)
        {
            txtItemDesc.text = $"Số lượng: {item.quantity}";
        }

        tooltipPanel.SetActive(true);

        // Đặt vị trí tooltip
        if (useFixedPosition && fixedAnchor != null)
            tooltipRect.position = fixedAnchor.position;
        else
            MoveTooltipToMouse();
    }

    /// <summary>
    /// Ẩn tooltip sau một khoảng delay ngắn — tránh flicker khi di chuột giữa các slot liền nhau.
    /// </summary>
    public void HideTooltip()
    {
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay(0.08f));
    }

    private System.Collections.IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
        hideCoroutine = null;
    }

    /// <summary>Tắt Raycast Target trên toàn bộ con của tooltip (ngăn bị che slot).</summary>
    private void DisableAllRaycastTargets()
    {
        if (tooltipPanel == null) return;
        foreach (Graphic g in tooltipPanel.GetComponentsInChildren<Graphic>(true))
        {
            g.raycastTarget = false;
        }
    }

    // ──────────────────────────────────────────────
    //  Private Helpers
    // ──────────────────────────────────────────────

    /// <summary>Di chuyển tooltip theo vị trí chuột, tự căn chỉnh để không ra ngoài màn hình.</summary>
    private void MoveTooltipToMouse()
    {
        if (tooltipRect == null || parentCanvas == null) return;

        // Chuyển tọa độ chuột (screen space) sang local space của Canvas
        Vector2 mousePos = Input.mousePosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.GetComponent<RectTransform>(),
            mousePos,
            parentCanvas.worldCamera,
            out Vector2 localPos
        );

        // Áp dụng offset
        Vector2 targetPos = localPos + offset;

        // Giữ tooltip trong giới hạn màn hình
        ClampToCanvas(ref targetPos);

        tooltipRect.anchoredPosition = targetPos;
    }

    /// <summary>Đẩy tooltip vào trong Canvas nếu bị tràn ra ngoài mép.</summary>
    private void ClampToCanvas(ref Vector2 pos)
    {
        if (parentCanvas == null || tooltipRect == null) return;

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        Vector2 canvasSize  = canvasRect.sizeDelta;
        Vector2 tooltipSize = tooltipRect.sizeDelta;

        float halfW = canvasSize.x * 0.5f;
        float halfH = canvasSize.y * 0.5f;

        // Giới hạn X
        float minX = -halfW + tooltipSize.x * 0.5f;
        float maxX =  halfW - tooltipSize.x * 0.5f;
        // Giới hạn Y
        float minY = -halfH + tooltipSize.y * 0.5f;
        float maxY =  halfH - tooltipSize.y * 0.5f;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
    }

    // ── Chuyển Enum sang tên Tiếng Việt ──────────
    private string GetTypeName(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon:     return "Vũ khí";
            case ItemType.Armor:      return "Giáp";
            case ItemType.Accessory:  return "Phụ kiện";
            case ItemType.Consumable: return "Tiêu hao";
            default:                  return type.ToString();
        }
    }

    private string GetRarityName(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return "Thường";
            case ItemRarity.Rare:   return "Hiếm";
            case ItemRarity.Epic:   return "Sử thi";
            default:                return rarity.ToString();
        }
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return new Color(0.85f, 0.85f, 0.85f); // Xám sáng
            case ItemRarity.Rare:   return new Color(0.3f, 0.6f, 1f);      // Xanh dương
            case ItemRarity.Epic:   return new Color(0.7f, 0.3f, 1f);      // Tím
            default:                return Color.white;
        }
    }
}
