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

    [Header("Tùy chỉnh kích thước")]
    [Tooltip("Hệ số phóng to tooltip. Mặc định = 1.5. Tăng lên để to hơn, giảm xuống để nhỏ hơn.")]
    [Range(0.5f, 4f)]
    [SerializeField] private float tooltipScale = 2.5f;


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

        // FIX: GetComponentInParent có thể trả null nếu script không nằm trực tiếp trong Canvas
        parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null)
            parentCanvas = FindFirstObjectByType<Canvas>();

        // Tự động tắt Raycast Target trên toàn bộ tooltip (panel + text)
        DisableAllRaycastTargets();
    }

    void Start()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
            tooltipPanel.transform.localScale = Vector3.one * tooltipScale;
        }
        ValidateSetup();
    }

    /// <summary>Kiểm tra toàn bộ setup và in log rõ ràng ra Console.</summary>
    [ContextMenu("Validate Tooltip Setup")]
    public void ValidateSetup()
    {
        bool ok = true;
        if (tooltipPanel == null)
        {
            Debug.LogError("[ItemTooltipUI] ❌ tooltipPanel chưa được gán trong Inspector!");
            ok = false;
        }
        if (txtItemName == null)  { Debug.LogWarning("[ItemTooltipUI] ⚠️ txtItemName chưa gán."); }
        if (txtItemType == null)  { Debug.LogWarning("[ItemTooltipUI] ⚠️ txtItemType chưa gán."); }
        if (txtItemStats == null) { Debug.LogWarning("[ItemTooltipUI] ⚠️ txtItemStats chưa gán."); }
        if (parentCanvas == null)
        {
            Debug.LogError("[ItemTooltipUI] ❌ Không tìm thấy Canvas nào trong Scene!");
            ok = false;
        }
        if (tooltipRect == null && tooltipPanel != null)
        {
            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            if (tooltipRect == null)
            {
                Debug.LogError("[ItemTooltipUI] ❌ tooltipPanel không có RectTransform!");
                ok = false;
            }
        }
        if (ok)
            Debug.Log("[ItemTooltipUI] ✅ Setup hợp lệ — Tooltip sẵn sàng hoạt động.");
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

        // Áp dụng scale (cho phép điều chỉnh runtime trong Inspector)
        tooltipPanel.transform.localScale = Vector3.one * tooltipScale;

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
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        // FIX: Chỉ dùng coroutine nếu GameObject đang active, tránh lỗi silent
        if (gameObject.activeInHierarchy)
            hideCoroutine = StartCoroutine(HideAfterDelay(0.08f));
        else if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
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

        Camera uiCam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();

        // Chuyển vị trí chuột → local space của Canvas
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.mousePosition, uiCam, out Vector2 localPoint))
            return;

        // Áp dụng offset (đơn vị = canvas pixels)
        localPoint += offset;

        // Chuyển local canvas → world space rồi gán vào tooltip
        // Dùng TransformPoint thay vì anchoredPosition để tránh lỗi anchor/pivot khác nhau
        tooltipRect.position = canvasRect.TransformPoint(new Vector3(localPoint.x, localPoint.y, 0f));

        // Đẩy vào trong màn hình nếu bị tràn mép
        ClampToScreen(uiCam);
    }

    /// <summary>Đẩy tooltip vào trong màn hình nếu bị tràn ra ngoài mép (dựa trên corners thực tế).</summary>
    private void ClampToScreen(Camera uiCam)
    {
        if (tooltipRect == null || parentCanvas == null) return;

        // Lấy 4 góc thực tế của tooltip (sau ContentSizeFitter tính kích thước xong)
        Vector3[] corners = new Vector3[4];
        tooltipRect.GetWorldCorners(corners);

        // corners[0]=bottom-left, corners[2]=top-right
        Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(uiCam, corners[0]);
        Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(uiCam, corners[2]);

        float shiftX = 0f, shiftY = 0f;
        if (screenMin.x < 0)             shiftX = -screenMin.x;
        if (screenMax.x > Screen.width)  shiftX = Screen.width - screenMax.x;
        if (screenMin.y < 0)             shiftY = -screenMin.y;
        if (screenMax.y > Screen.height) shiftY = Screen.height - screenMax.y;

        if (shiftX == 0f && shiftY == 0f) return;

        // ScreenSpaceOverlay: world position = screen pixels → shift trực tiếp
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            tooltipRect.position += new Vector3(shiftX, shiftY, 0f);
        }
        else
        {
            // Camera/World Space: chuyển screen pixels → canvas local units rồi dịch chuyển
            RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
            float sx = Screen.width  > 0 ? shiftX / Screen.width  : 0f;
            float sy = Screen.height > 0 ? shiftY / Screen.height : 0f;
            tooltipRect.position += canvasRect.TransformVector(
                new Vector3(sx * canvasRect.rect.width, sy * canvasRect.rect.height, 0f));
        }
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
