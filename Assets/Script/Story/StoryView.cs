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

    [Header("Middle")]
    [SerializeField] private ScrollRect storyScrollRect;
    [SerializeField] private TextMeshProUGUI txtStoryLog;
    [SerializeField] private Button btnAdvance;
    [SerializeField] private GameObject iconNextIndicator;

    [Header("Bottom")]
    [SerializeField] private Button[] choiceButtons = new Button[3];
    [SerializeField] private TextMeshProUGUI[] choiceTexts = new TextMeshProUGUI[3];

    private void Awake()
    {
        SetNextIndicatorVisible(false);
        SetChoiceButtonsVisible(false);
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
    }

    public void SetStoryText(string text)
    {
        if (txtStoryLog != null)
        {
            txtStoryLog.text = text;
            Canvas.ForceUpdateCanvases();
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
        Canvas.ForceUpdateCanvases();
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
        SetChoiceButtonsVisible(true);

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
}
