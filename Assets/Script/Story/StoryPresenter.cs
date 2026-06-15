using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class StoryPresenter : MonoBehaviour
{
    [SerializeField] private StoryView view;
    [SerializeField] private bool useMockStoryOnStart = true;
    [SerializeField] private float characterDelay = 0.03f;
    [SerializeField] private float linePause = 0.6f;
    [SerializeField] private float chunkSize = 70f;
    [SerializeField] private string richTextOpeningTag = string.Empty;
    [SerializeField] private string richTextClosingTag = string.Empty;

    private StoryData currentData;
    private Coroutine playbackCoroutine;
    private bool isTyping;
    private bool skipTyping;
    private bool waitingForAdvance;
    private bool awaitingChoice;

    private readonly Queue<StoryLineData> pendingLines = new Queue<StoryLineData>();

    private void Start()
    {
        if (view == null)
        {
            view = GetComponent<StoryView>();
        }

        if (view != null)
        {
            view.BindAdvance(HandleAdvancePressed);
        }

        if (useMockStoryOnStart)
        {
            StartMockStory();
        }
    }

    private void Update()
    {
        if (ConsumeAdvanceInput())
        {
            HandleAdvancePressed();
        }
    }

    private static bool ConsumeAdvanceInput()
    {
        bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        return mouseClicked || spacePressed;
    }

    public void StartMockStory()
    {
        StartStory(CreateMockData());
    }

    public void StartStory(StoryData data)
    {
        currentData = data;

        if (view == null || currentData == null || currentData.node == null)
        {
            return;
        }

        StopCurrentPlayback();
        view.SetStoryText(string.Empty);
        view.SetNextIndicatorVisible(false);
        view.SetChoiceButtonsVisible(false);
        view.SetChoiceInteractable(false);
        view.SetAdvanceInteractable(true);
        view.SetCharacterState(currentData.node.character);
        pendingLines.Clear();

        if (!string.IsNullOrEmpty(currentData.node.backgroundKey))
        {
            Sprite backgroundSprite = Resources.Load<Sprite>(currentData.node.backgroundKey);
            if (backgroundSprite != null)
            {
                view.SetBackground(backgroundSprite);
            }
        }

        for (int index = 0; index < currentData.node.lines.Count; index++)
        {
            pendingLines.Enqueue(currentData.node.lines[index]);
        }

        playbackCoroutine = StartCoroutine(PlayStoryRoutine());
    }

    public void SetStoryData(StoryData data)
    {
        StartStory(data);
    }

    private IEnumerator PlayStoryRoutine()
    {
        while (pendingLines.Count > 0)
        {
            StoryLineData line = pendingLines.Dequeue();
            yield return TypeLineRoutine(line.text);

            if (line.pauseAfter > 0f)
            {
                yield return new WaitForSeconds(line.pauseAfter);
            }
        }

        view.SetNextIndicatorVisible(false);
        view.SetChoiceButtonsVisible(true);
        view.SetChoiceInteractable(true);
        view.SetChoices(currentData.node.choices != null ? currentData.node.choices.ToArray() : null, OnChoiceSelected);
        awaitingChoice = true;
    }

    private IEnumerator TypeLineRoutine(string text)
    {
        isTyping = true;
        skipTyping = false;
        waitingForAdvance = false;

        if (string.IsNullOrEmpty(text))
        {
            isTyping = false;
            yield break;
        }

        string visibleBuffer = string.Empty;
        string[] chunks = SplitForDisplay(text, Mathf.Max(1, Mathf.RoundToInt(chunkSize)));

        for (int chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
        {
            string chunk = chunks[chunkIndex];
            string renderedChunk = string.Empty;
            int characterIndex = 0;

            while (characterIndex < chunk.Length)
            {
                if (skipTyping)
                {
                    renderedChunk = chunk;
                    break;
                }

                renderedChunk += chunk[characterIndex];
                view.SetStoryText(visibleBuffer + renderedChunk);
                characterIndex++;
                yield return new WaitForSeconds(characterDelay);
            }

            view.SetStoryText(visibleBuffer + renderedChunk);
            visibleBuffer += renderedChunk;

            if (chunkIndex < chunks.Length - 1)
            {
                visibleBuffer += "\n";
                view.SetStoryText(visibleBuffer);
            }
        }

        isTyping = false;
        waitingForAdvance = true;
        view.SetNextIndicatorVisible(true);

        while (waitingForAdvance)
        {
            yield return null;
        }

        view.SetNextIndicatorVisible(false);
    }

    private void HandleAdvancePressed()
    {
        if (awaitingChoice)
        {
            return;
        }

        if (isTyping)
        {
            skipTyping = true;
            return;
        }

        if (waitingForAdvance)
        {
            waitingForAdvance = false;
        }
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        if (currentData == null || currentData.node == null || currentData.node.choices == null)
        {
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= currentData.node.choices.Count)
        {
            return;
        }

        StoryChoiceData choice = currentData.node.choices[choiceIndex];
        Debug.Log("Story choice selected: " + choice.label + " -> " + choice.nextNodeId);
    }

    private void StopCurrentPlayback()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        isTyping = false;
        skipTyping = false;
        waitingForAdvance = false;
        awaitingChoice = false;
    }

    private static string[] SplitForDisplay(string source, int maxChunkSize)
    {
        if (string.IsNullOrEmpty(source) || source.Length <= maxChunkSize)
        {
            return new[] { source };
        }

        List<string> chunks = new List<string>();
        int startIndex = 0;

        while (startIndex < source.Length)
        {
            int length = Mathf.Min(maxChunkSize, source.Length - startIndex);
            int splitIndex = source.LastIndexOf(' ', startIndex + length - 1, length);

            if (splitIndex <= startIndex)
            {
                splitIndex = startIndex + length;
            }

            string chunk = source.Substring(startIndex, splitIndex - startIndex).Trim();
            if (!string.IsNullOrEmpty(chunk))
            {
                chunks.Add(chunk);
            }

            startIndex = splitIndex;
            while (startIndex < source.Length && source[startIndex] == ' ')
            {
                startIndex++;
            }
        }

        return chunks.ToArray();
    }

    private StoryData CreateMockData()
    {
        StoryData data = new StoryData
        {
            title = "Chapter 1"
        };

        data.node = new StoryNodeData
        {
            nodeId = "intro_01",
            backgroundKey = string.Empty,
            character = new StoryCharacterState
            {
                characterName = "Player_Name",
                level = 7,
                hp = 84,
                gold = 120
            },
            lines = new List<StoryLineData>
            {
                new StoryLineData
                {
                    text = "Bầu không khí trong tàn tích cổ xưa nặng như chì. Khi bạn bước vào hành lang đá, những ký tự rune sáng lên từng nhịp, như thể ngôi đền đang quan sát mọi chuyển động của bạn.",
                    pauseAfter = 0.25f
                },
                new StoryLineData
                {
                    text = "Một tiếng thì thầm vang lên từ bóng tối: 'Nếu muốn sống sót, hãy chọn con đường của ngọn lửa, bóng tối, hay máu.'",
                    pauseAfter = 0.25f
                }
            },
            choices = new List<StoryChoiceData>
            {
                new StoryChoiceData { label = "Tiến lên", description = "Đi thẳng vào đại sảnh", nextNodeId = "advance_hall" },
                new StoryChoiceData { label = "Quan sát", description = "Kiểm tra bẫy và manh mối", nextNodeId = "inspect_room" },
                new StoryChoiceData { label = "Rút lui", description = "Tạm thời lùi lại", nextNodeId = "retreat" }
            }
        };

        return data;
    }
}
