using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoryView : MonoBehaviour
{
    [Header("Top")]
    [SerializeField] private Image imgBackground;
    [SerializeField] private TextMeshProUGUI txtCharacterName;
    [SerializeField] private TextMeshProUGUI txtCharacterLevel;
    [SerializeField] private TextMeshProUGUI txtCharacterHP;
    [SerializeField] private TextMeshProUGUI txtCharacterGold;
    [SerializeField] private TextMeshProUGUI txtCharacterXP;

    [Header("Middle")]
    [SerializeField] private ScrollRect storyScrollRect;
    [SerializeField] private TextMeshProUGUI txtStoryLog;
    [SerializeField] private Button btnAdvance;
    [SerializeField] private GameObject iconNextIndicator;

    [Header("Bottom")]
    [SerializeField] private GameObject panelActiveOption;
    [SerializeField] private Button btnSwitchToInput;
    [SerializeField] private Button[] choiceButtons = new Button[3];
    [SerializeField] private TextMeshProUGUI[] choiceTexts = new TextMeshProUGUI[3];

    [Header("Custom Text Input")]
    [SerializeField] private GameObject panelTextInput;
    [SerializeField] private Button btnSwitchToOptions;
    [SerializeField] private TMP_InputField inputStoryAction;
    [SerializeField] private Button btnSubmitAction;

    [Header("Navigation")]
    [SerializeField] private Button btnBack;

    [Header("Boss Encounter Popup")]
    [Tooltip("Panel popup boss encounter — mặc định SetActive(false) trong Scene")]
    [SerializeField] private GameObject bossEncounterPanel;
    [SerializeField] private Image bossEncounterOverlay;          // nền tối mờ
    [SerializeField] private TextMeshProUGUI txtBossName;          // tên boss
    [SerializeField] private TextMeshProUGUI txtBossRarity;        // rarity badge
    [SerializeField] private TextMeshProUGUI txtBossLevel;         // "Lv. 12"
    [SerializeField] private Button btnFight;                      // Chiến đấu
    [SerializeField] private Button btnFlee;                       // Bỏ chạy

    private Action<string> onSubmitCallback;

    private void Awake()
    {
        SetNextIndicatorVisible(false);
        SetActiveOptionsPanelVisible(false);
        SetInputPanelVisible(false);
        HideBossEncounterPopup();
    }

    public void BindAdvance(Action onAdvance)
    {
        if (btnAdvance == null)
        {
            return;
        }

        btnAdvance.onClick.RemoveAllListeners();
        if (onAdvance != null)
        {
            btnAdvance.onClick.AddListener(() => onAdvance());
        }
    }

    public void BindBack(Action onBack)
    {
        if (btnBack == null) return;
        btnBack.onClick.RemoveAllListeners();
        if (onBack != null)
            btnBack.onClick.AddListener(() => onBack());
    }

    public void BindSwitchModes(Action onSwitchToInput, Action onSwitchToOptions)
    {
        if (btnSwitchToInput != null)
        {
            btnSwitchToInput.onClick.RemoveAllListeners();
            if (onSwitchToInput != null) btnSwitchToInput.onClick.AddListener(() => onSwitchToInput());
        }

        if (btnSwitchToOptions != null)
        {
            btnSwitchToOptions.onClick.RemoveAllListeners();
            if (onSwitchToOptions != null) btnSwitchToOptions.onClick.AddListener(() => onSwitchToOptions());
        }
    }

    // ══════════════════════════════════════════════════════════════
    // BOSS ENCOUNTER POPUP
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Bước 1: Chỉ hiện Overlay (hình con mắt/nền quái vật) với độ mờ hoàn toàn.
    /// Gọi trước ShowBossPanel() để tạo hiệu ứng xuất hiện dần.
    /// </summary>
    public void ShowBossOverlay()
    {
        if (bossEncounterOverlay == null) return;

        bossEncounterOverlay.gameObject.SetActive(true);
        // Alpha = 1 — hiện đầy đủ, không mờ
        Color c = bossEncounterOverlay.color;
        c.a = 1f;
        bossEncounterOverlay.color = c;
    }

    /// <summary>
    /// Bước 2: Hiện Panel thông tin boss và bind 2 nút (sau khi Overlay đã hiện).
    /// </summary>
    public void ShowBossPanel(
        string bossName, string bossRarity, int bossLevel,
        Action onFight, Action onFlee)
    {
        // Điền thông tin boss
        if (txtBossName   != null) txtBossName.text   = bossName;
        if (txtBossRarity != null) txtBossRarity.text = bossRarity.ToUpper();
        if (txtBossLevel  != null) txtBossLevel.text  = "Lv. " + bossLevel;

        // Bind nút
        if (btnFight != null)
        {
            btnFight.onClick.RemoveAllListeners();
            btnFight.onClick.AddListener(() => onFight?.Invoke());
        }
        if (btnFlee != null)
        {
            btnFlee.onClick.RemoveAllListeners();
            btnFlee.onClick.AddListener(() => onFlee?.Invoke());
        }

        // Hiện panel
        if (bossEncounterPanel != null)
            bossEncounterPanel.SetActive(true);
    }

    /// <summary>Ẩn toàn bộ popup Boss Encounter (overlay + panel).</summary>
    public void HideBossEncounterPopup()
    {
        if (bossEncounterPanel   != null) bossEncounterPanel.SetActive(false);
        if (bossEncounterOverlay != null) bossEncounterOverlay.gameObject.SetActive(false);
    }

    // ═ (giữ lại để không break gì cũ) ShowBossEncounterPopup đã được thay bằng ShowBossOverlay + ShowBossPanel
    [System.Obsolete("Dùng ShowBossOverlay() + ShowBossPanel() thay thế.")]
    public void ShowBossEncounterPopup(
        string bossName, string bossRarity, int bossLevel,
        Action onFight, Action onFlee)
    {
        ShowBossOverlay();
        ShowBossPanel(bossName, bossRarity, bossLevel, onFight, onFlee);
    }

    public void SetBackground(Sprite sprite)
    {
        if (imgBackground != null)
        {
            imgBackground.sprite = sprite;
        }
    }

    public void SetCharacterState(StoryCharacterState state)
    {
        if (state == null)
        {
            return;
        }

        if (txtCharacterName != null)
        {
            txtCharacterName.text = state.characterName;
        }

        if (txtCharacterLevel != null)
        {
            txtCharacterLevel.text = "Lv. " + state.level;
        }

        if (txtCharacterHP != null)
        {
            txtCharacterHP.text = "HP " + state.hp;
        }

        if (txtCharacterGold != null)
        {
            txtCharacterGold.text = "Gold " + state.gold;
        }

        if (txtCharacterXP != null)
        {
            txtCharacterXP.text = $"XP {state.xp}/{state.maxXP}";
        }
    }

    public void SetStoryText(string text)
    {
        if (txtStoryLog != null)
        {
            txtStoryLog.text = text;
            try
            {
                Canvas.ForceUpdateCanvases();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[StoryView] Canvas.ForceUpdateCanvases warning: " + ex.Message);
            }

            if (storyScrollRect != null)
            {
                storyScrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }

    public void AppendStoryText(string text)
    {
        if (txtStoryLog == null)
        {
            return;
        }

        txtStoryLog.text += text;
        try
        {
            Canvas.ForceUpdateCanvases();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[StoryView] Canvas.ForceUpdateCanvases warning: " + ex.Message);
        }

        if (storyScrollRect != null)
        {
            storyScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    public void SetNextIndicatorVisible(bool visible)
    {
        if (iconNextIndicator != null)
        {
            iconNextIndicator.SetActive(visible);
        }
    }

    public void SetActiveOptionsPanelVisible(bool visible)
    {
        if (panelActiveOption != null)
        {
            panelActiveOption.SetActive(visible);
        }
        else
        {
            SetChoiceButtonsVisible(visible);
        }
    }

    public void SetChoiceButtonsVisible(bool visible)
    {
        if (choiceButtons == null)
        {
            return;
        }

        for (int index = 0; index < choiceButtons.Length; index++)
        {
            if (choiceButtons[index] != null)
            {
                choiceButtons[index].gameObject.SetActive(visible);
            }
        }
    }

    public void SetChoices(StoryChoiceData[] choices, Action<int> onChoiceSelected)
    {

        for (int index = 0; index < choiceButtons.Length; index++)
        {
            if (choiceButtons[index] == null)
            {
                continue;
            }

            choiceButtons[index].onClick.RemoveAllListeners();
            choiceButtons[index].gameObject.SetActive(false);

            if (choiceTexts != null && index < choiceTexts.Length && choiceTexts[index] != null)
            {
                choiceTexts[index].text = string.Empty;
            }
        }

        if (choices == null)
        {
            return;
        }

        int count = Mathf.Min(choices.Length, choiceButtons.Length);
        for (int index = 0; index < count; index++)
        {
            StoryChoiceData choice = choices[index];
            if (choiceButtons[index] == null || choice == null)
            {
                continue;
            }

            TextMeshProUGUI label = ResolveChoiceLabel(index);
            if (label != null)
            {
                label.text = choice.label;
            }

            int capturedIndex = index;
            choiceButtons[index].gameObject.SetActive(true);
            choiceButtons[index].onClick.AddListener(() => onChoiceSelected?.Invoke(capturedIndex));
        }
    }

    private TextMeshProUGUI ResolveChoiceLabel(int index)
    {
        if (choiceTexts != null && index < choiceTexts.Length && choiceTexts[index] != null)
        {
            return choiceTexts[index];
        }

        if (choiceButtons == null || index < 0 || index >= choiceButtons.Length)
        {
            return null;
        }

        Button button = choiceButtons[index];
        if (button == null)
        {
            return null;
        }

        return button.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void SetAdvanceInteractable(bool interactable)
    {
        if (btnAdvance != null)
        {
            btnAdvance.interactable = interactable;
        }
    }

    public void SetChoiceInteractable(bool interactable)
    {
        if (choiceButtons == null)
        {
            return;
        }

        for (int index = 0; index < choiceButtons.Length; index++)
        {
            if (choiceButtons[index] != null)
            {
                choiceButtons[index].interactable = interactable;
            }
        }
    }

    public void SetInputPanelVisible(bool visible)
    {
        if (panelTextInput != null)
        {
            panelTextInput.SetActive(visible);
        }
    }

    public void BindSubmitAction(Action<string> onSubmit)
    {
        onSubmitCallback = onSubmit;

        if (btnSubmitAction != null)
        {
            btnSubmitAction.onClick.RemoveAllListeners();
            btnSubmitAction.onClick.AddListener(OnSubmitClicked);
        }

        if (inputStoryAction != null)
        {
            inputStoryAction.onSubmit.RemoveAllListeners();
            inputStoryAction.onSubmit.AddListener(text => OnSubmitClicked());
        }
    }

    private void OnSubmitClicked()
    {
        string text = GetInputValue();
        if (!string.IsNullOrWhiteSpace(text))
        {
            onSubmitCallback?.Invoke(text.Trim());
        }
    }

    public string GetInputValue()
    {
        return inputStoryAction != null ? inputStoryAction.text : string.Empty;
    }

    public void ClearInputField()
    {
        if (inputStoryAction != null)
        {
            inputStoryAction.text = string.Empty;
        }
    }

    public void SetInputInteractable(bool interactable)
    {
        if (inputStoryAction != null)
        {
            inputStoryAction.interactable = interactable;
        }

        if (btnSubmitAction != null)
        {
            btnSubmitAction.interactable = interactable;
        }
    }
}
