using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameShared.DTOs.Character;

/// <summary>
/// View cho ProfileScene — quản lý toàn bộ UI reference và render từng tab.
/// Pattern nhất quán với StoryView và BattleView.
/// Attach vào root GameObject của ProfileScene Canvas.
/// </summary>
public class ProfileView : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    //  HEADER — Thông tin nhân vật cố định trên mọi tab
    // ─────────────────────────────────────────────────────────
    [Header("Header")]
    [SerializeField] private TextMeshProUGUI txtCharacterName;
    [SerializeField] private TextMeshProUGUI txtClassAndLevel;
    [SerializeField] private TextMeshProUGUI txtGold;
    [SerializeField] private TextMeshProUGUI txtStatus;

    // ─────────────────────────────────────────────────────────
    //  TAB NAVIGATION
    // ─────────────────────────────────────────────────────────
    [Header("Tab Buttons (order: Overview, Stats, Equipment, Titles, History)")]
    [SerializeField] private Button[] tabButtons = new Button[5];
    [SerializeField] private GameObject[] tabPanels = new GameObject[5];

    private static readonly string[] TabNames = { "Tổng Quan", "Chỉ Số", "Trang Bị", "Danh Hiệu", "Lịch Sử" };
    private int currentTab = 0;

    // ─────────────────────────────────────────────────────────
    //  TAB 0 — TỔNG QUAN
    // ─────────────────────────────────────────────────────────
    [Header("Tab 0 — Tổng Quan")]
    [SerializeField] private Image imgHPBar;
    [SerializeField] private TextMeshProUGUI txtHP;
    [SerializeField] private Image imgMPBar;
    [SerializeField] private TextMeshProUGUI txtMP;
    [SerializeField] private Image imgEXPBar;
    [SerializeField] private TextMeshProUGUI txtEXP;
    [SerializeField] private TextMeshProUGUI txtLocation;

    // ─────────────────────────────────────────────────────────
    //  TAB 1 — CHỈ SỐ
    // ─────────────────────────────────────────────────────────
    [Header("Tab 1 — Chỉ Số Chiến Đấu")]
    [SerializeField] private TextMeshProUGUI txtAttack;
    [SerializeField] private TextMeshProUGUI txtDefense;
    [SerializeField] private TextMeshProUGUI txtCritRate;
    [SerializeField] private TextMeshProUGUI txtLuckyRate;
    [SerializeField] private TextMeshProUGUI txtSpeed;
    [SerializeField] private TextMeshProUGUI txtEvasion;
    [SerializeField] private TextMeshProUGUI txtMagicResist;

    // ─────────────────────────────────────────────────────────
    //  TAB 2 — TRANG BỊ
    // ─────────────────────────────────────────────────────────
    [Header("Tab 2 — Trang Bị")]
    [SerializeField] private Transform equipmentSlotContainer;
    [SerializeField] private GameObject equipmentSlotPrefab; // prefab: EquipmentSlotUI

    // ─────────────────────────────────────────────────────────
    //  TAB 3 — DANH HIỆU
    // ─────────────────────────────────────────────────────────
    [Header("Tab 3 — Danh Hiệu")]
    [SerializeField] private Transform titleListContainer;
    [SerializeField] private GameObject titleEntryPrefab;   // prefab: TitleEntryUI

    // ─────────────────────────────────────────────────────────
    //  TAB 4 — LỊCH SỬ PHIÊU LƯU
    // ─────────────────────────────────────────────────────────
    [Header("Tab 4 — Lịch Sử Phiêu Lưu")]
    [SerializeField] private Transform historyListContainer;
    [SerializeField] private GameObject historyEntryPrefab; // prefab: HistoryEntryUI

    // ─────────────────────────────────────────────────────────
    //  CLOSE BUTTON
    // ─────────────────────────────────────────────────────────
    [Header("Navigation")]
    [SerializeField] private Button btnClose;

    // ══════════════════════════════════════════════════════════
    //  Unity Lifecycle
    // ══════════════════════════════════════════════════════════

    private void Awake()
    {
        // Gắn sự kiện tab
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int captured = i;
            if (tabButtons[captured] != null)
            {
                tabButtons[captured].onClick.AddListener(() => SwitchTab(captured));
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  PUBLIC API — được gọi bởi ProfilePresenter
    // ══════════════════════════════════════════════════════════

    /// <summary>Bind nút Close để ProfilePresenter xử lý điều hướng.</summary>
    public void BindClose(System.Action onClose)
    {
        if (btnClose != null)
        {
            Debug.Log("[ProfileView] BindClose thành công! Đã gắn sự kiện Click vào btnClose.");
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(() => {
                Debug.Log("[ProfileView] Người chơi đã click nút Close!");
                onClose?.Invoke();
            });
        }
        else
        {
            Debug.LogError("[ProfileView] BindClose thất bại: Biến btnClose trong Inspector đang bị NULL (None)!");
        }
    }

    /// <summary>Render toàn bộ dữ liệu Profile và chuyển về tab đầu tiên.</summary>
    public void Render(ProfileData data)
    {
        if (data == null) return;

        RenderHeader(data.overview);
        RenderOverview(data.overview);
        RenderStats(data.stats);
        RenderEquipment(data.equipment);
        RenderTitles(data.titles);
        RenderHistory(data.history);

        SwitchTab(0);
    }

    // ══════════════════════════════════════════════════════════
    //  TAB SWITCHING
    // ══════════════════════════════════════════════════════════

    public void SwitchTab(int index)
    {
        currentTab = index;

        for (int i = 0; i < tabPanels.Length; i++)
        {
            if (tabPanels[i] != null)
            {
                tabPanels[i].SetActive(i == index);
            }
        }

        // Visual feedback: highlight tab đang chọn (thay đổi màu button)
        for (int i = 0; i < tabButtons.Length; i++)
        {
            if (tabButtons[i] == null) continue;
            ColorBlock cb = tabButtons[i].colors;
            cb.normalColor = (i == index) ? new Color(0.3f, 0.6f, 1f) : Color.white;
            tabButtons[i].colors = cb;
        }
    }

    // ══════════════════════════════════════════════════════════
    //  RENDER METHODS
    // ══════════════════════════════════════════════════════════

    private void RenderHeader(ProfileOverviewData data)
    {
        if (data == null) return;
        SetText(txtCharacterName, data.characterName);
        SetText(txtClassAndLevel, $"{data.className} · Lv.{data.level}");
        SetText(txtGold, $"Gold  {data.gold}");
        SetText(txtStatus, data.status);
    }

    private void RenderOverview(ProfileOverviewData data)
    {
        if (data == null) return;

        // HP bar
        SetBar(imgHPBar, data.hp, data.maxHp);
        SetText(txtHP, $"HP  {data.hp} / {data.maxHp}");

        // MP bar
        SetBar(imgMPBar, data.mp, data.maxMp);
        SetText(txtMP, $"MP  {data.mp} / {data.maxMp}");

        // EXP bar
        SetBar(imgEXPBar, data.experience, data.experienceToNextLevel);
        SetText(txtEXP, $"EXP  {data.experience} / {data.experienceToNextLevel}");

        // Location
        SetText(txtLocation, $"Vị Trí: {data.locationId}");
    }

    private void RenderStats(ProfileStatsData data)
    {
        if (data == null) return;
        SetText(txtAttack,      $"ATK        {data.attack}");
        SetText(txtDefense,     $"DEF        {data.defense}");
        SetText(txtCritRate,    $"CRIT       {data.criticalRate * 100f:F1}%");
        SetText(txtLuckyRate,   $"LUCKY      {data.luckyRate * 100f:F1}%");
        SetText(txtSpeed,       $"SPD        {data.speed:F0}");
        SetText(txtEvasion,     $"EVASION    {data.evasionRate * 100f:F1}%");
        SetText(txtMagicResist, $"MAG.RES    {data.magicResist:F0}");
    }

    private void RenderEquipment(ProfileEquipmentData data)
    {
        if (data == null || equipmentSlotContainer == null || equipmentSlotPrefab == null) return;

        ClearChildren(equipmentSlotContainer);

        foreach (ProfileEquippedSlot slot in data.slots)
        {
            GameObject go = Instantiate(equipmentSlotPrefab, equipmentSlotContainer);
            EquipmentSlotUI slotUI = go.GetComponent<EquipmentSlotUI>();
            if (slotUI != null)
            {
                slotUI.SetSlot(slot);
            }
        }
    }

    private void RenderTitles(ProfileTitlesData data)
    {
        if (data == null || titleListContainer == null || titleEntryPrefab == null) return;

        ClearChildren(titleListContainer);

        foreach (ProfileTitleEntry entry in data.titles)
        {
            GameObject go = Instantiate(titleEntryPrefab, titleListContainer);
            TitleEntryUI entryUI = go.GetComponent<TitleEntryUI>();
            if (entryUI != null)
            {
                entryUI.SetEntry(entry);
            }
        }
    }

    private void RenderHistory(ProfileHistoryData data)
    {
        if (data == null || historyListContainer == null || historyEntryPrefab == null) return;

        ClearChildren(historyListContainer);

        foreach (AdventureRecord record in data.records)
        {
            GameObject go = Instantiate(historyEntryPrefab, historyListContainer);
            HistoryEntryUI entryUI = go.GetComponent<HistoryEntryUI>();
            if (entryUI != null)
            {
                entryUI.SetRecord(record);
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════

    private static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null) label.text = value;
    }

    private static void SetBar(Image bar, int current, int max)
    {
        if (bar == null) return;
        bar.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
