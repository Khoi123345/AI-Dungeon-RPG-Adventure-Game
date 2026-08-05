using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using GameShared.DTOs.Story;

public class StoryPresenter : MonoBehaviour
{
    [SerializeField] private StoryView view;
    [SerializeField] private bool useMockStoryOnStart = false;
    [SerializeField] private float characterDelay = 0.03f;
    [SerializeField] private float linePause = 0.6f;
    [SerializeField] private float chunkSize = 70f;
    [SerializeField] private string richTextOpeningTag = string.Empty;
    [SerializeField] private string richTextClosingTag = string.Empty;

    [Header("Navigation")]
    [SerializeField] private string menuScene = "Menu";
    [SerializeField] private string battleScene = "BattleScene";

    [Header("Boss Encounter")]
    [Tooltip("Xác suất gặp boss sau mỗi hành động người chơi (0.0 - 1.0)")]
    [SerializeField] private float bossEncounterChance = 0.35f;

    private readonly StoryApiService storyApiService = new StoryApiService();

    private StoryData currentData;
    private Coroutine playbackCoroutine;
    private bool isTyping;
    private bool skipTyping;
    private bool waitingForAdvance;
    private bool awaitingChoice;
    private bool isBossPopupShowing;  // chặn advance khi popup boss đang hiện

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
            view.BindSubmitAction(HandleUserActionSubmitted);
            view.BindBack(OnBackClicked);
        }

        GameProgressService.EnsureInstance();

        if (IsMockMode)
        {
            StartMockStory();
        }
        else
        {
            StartRealStory();
        }
    }

    private bool IsMockMode => GameConfigSO.Instance != null ? GameConfigSO.Instance.useMockMode : useMockStoryOnStart;

    public async void StartRealStory()
    {
        if (view != null)
        {
            view.SetStoryText("<i><color=#AAAAAA>Đang kết nối AI Bedrock và khởi tạo hầm ngục...</color></i>");
            view.SetNextIndicatorVisible(false);
            view.SetChoiceButtonsVisible(false);
            view.SetInputPanelVisible(false);
        }

        string characterId = GameProgressService.Instance?.CurrentCharacter?.characterId;
        if (string.IsNullOrEmpty(characterId))
        {
            characterId = "demo_char_id";
        }

        Debug.Log($"[StoryPresenter] Gửi yêu cầu /story/start với characterId={characterId}");
        var response = await storyApiService.StartStoryAsync(characterId, "prologue");

        if (response != null && !string.IsNullOrEmpty(response.narrativeText))
        {
            Debug.Log($"[StoryPresenter] Nhận phản hồi mở đầu từ AI Bedrock (sessionId={response.sessionId})");
            GameProgressService.Instance?.SetCurrentStorySession(response.sessionId, response.currentNodeId, response.currentLocation);
            StoryData storyData = MapActionResponseToStoryData(response);
            StartStory(storyData);
        }
        else
        {
            Debug.LogWarning("[StoryPresenter] Không thể nhận response từ AI Bedrock. Chuyển sang dùng Mock Story...");
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
        StoryData storyData = GameProgressService.Instance != null
            ? GameProgressService.Instance.CreateStoryDemoData()
            : CreateMockData();

        StartStory(storyData);
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
        view.SetInputPanelVisible(false);
        view.ClearInputField();
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
        if (currentData != null && currentData.node != null && currentData.node.choices != null && currentData.node.choices.Count > 0)
        {
            view.SetChoices(currentData.node.choices.ToArray(), OnChoiceSelected);
        }
        view.SetInputPanelVisible(true);
        view.SetInputInteractable(true);
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
        // Không xử lý advance khi popup boss đang hiện
        if (isBossPopupShowing) return;

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

    private async void OnChoiceSelected(int choiceIndex)
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

        awaitingChoice = false;
        view.SetChoiceButtonsVisible(false);
        view.SetInputInteractable(false);
        view.SetInputPanelVisible(false);

        view.AppendStoryText($"\n\n<b>> Lựa chọn:</b> \"{choice.label}\"\n\n");

        if (IsMockMode)
        {
            if (GameProgressService.Instance != null)
            {
                string aiResponse = $"Bạn đã chọn: {choice.label}. Nhánh mới: {choice.nextNodeId}.";
                GameProgressService.Instance.RecordStoryAction(choiceIndex, choice.description, aiResponse);
            }
            Debug.Log("Story choice selected: " + choice.label + " -> " + choice.nextNodeId);
        }
        else
        {
            view.AppendStoryText("<i><color=#888888>[AI Bedrock đang suy nghĩ...]</color></i>\n");

            string characterId = GameProgressService.Instance?.CurrentCharacter?.characterId ?? "demo_char_id";
            string sessionId = GameProgressService.Instance?.CurrentStorySession?.sessionId ?? "";

            var response = await storyApiService.SendActionAsync(characterId, sessionId, choiceIndex, choice.label);

            if (response != null && !string.IsNullOrEmpty(response.narrativeText))
            {
                Debug.Log($"<color=#00FF00>[StoryPresenter] Nhận phản hồi từ AI Bedrock (Choice):</color>\n- triggerBattle: <b>{response.triggerBattle}</b>\n- bossId: <b>{response.bossId}</b>\n- location: <b>{response.currentLocation}</b>");

                GameProgressService.Instance?.SetCurrentStorySession(response.sessionId, response.currentNodeId, response.currentLocation);
                StoryData nextStoryData = MapActionResponseToStoryData(response);
                PlayNextStoryNode(nextStoryData);

                if (response.triggerBattle)
                {
                    Debug.Log($"<color=#FF5500><b>[StoryPresenter] AI CHÍNH THỨC KÍCH HOẠT TRẬN ĐÁNH BOSS!</b> BossId = '{response.bossId}'</color>");
                    if (gameObject.activeInHierarchy)
                    {
                        StartCoroutine(TriggerBossEncounterFromAi(response.bossId));
                    }
                }
                else
                {
                    Debug.Log("<color=#FFFF00>[StoryPresenter] AI Bedrock không kích hoạt trận đánh ở lượt này (triggerBattle = false).</color>");
                }
            }
            else
            {
                Debug.LogWarning("[StoryPresenter] AI Bedrock không phản hồi cho lựa chọn này.");
            }
        }
    }

    private async void HandleUserActionSubmitted(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText) || !awaitingChoice)
        {
            return;
        }

        awaitingChoice = false;
        view.SetChoiceButtonsVisible(false);
        view.SetInputInteractable(false);
        view.SetInputPanelVisible(false);
        view.ClearInputField();

        // Ghi nhận hành động vừa gõ của người chơi vào ô log hội thoại
        view.AppendStoryText($"\n\n<b>> Bạn:</b> \"{userText}\"\n\n");

        if (IsMockMode)
        {
            // Sinh dữ liệu cốt truyện tiếp theo từ văn bản gõ
            StoryData nextStoryData = GameProgressService.Instance != null
                ? GameProgressService.Instance.ExecuteCustomStoryAction(userText)
                : CreateMockCustomResponse(userText);

            PlayNextStoryNode(nextStoryData);

            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(TryTriggerBossEncounterDelayed());
            }
        }
        else
        {
            view.AppendStoryText("<i><color=#888888>[AI Bedrock đang suy nghĩ...]</color></i>\n");

            string characterId = GameProgressService.Instance?.CurrentCharacter?.characterId ?? "demo_char_id";
            string sessionId = GameProgressService.Instance?.CurrentStorySession?.sessionId ?? "";

            var response = await storyApiService.SendActionAsync(characterId, sessionId, choiceIndex: -1, playerInput: userText);

            if (response != null && !string.IsNullOrEmpty(response.narrativeText))
            {
                Debug.Log($"<color=#00FF00>[StoryPresenter] Nhận phản hồi từ AI Bedrock:</color>\n- triggerBattle: <b>{response.triggerBattle}</b>\n- bossId: <b>{response.bossId}</b>\n- location: <b>{response.currentLocation}</b>");

                GameProgressService.Instance?.SetCurrentStorySession(response.sessionId, response.currentNodeId, response.currentLocation);
                StoryData nextStoryData = MapActionResponseToStoryData(response);
                PlayNextStoryNode(nextStoryData);

                if (response.triggerBattle)
                {
                    Debug.Log($"<color=#FF5500><b>[StoryPresenter] AI CHÍNH THỨC KÍCH HOẠT TRẬN ĐÁNH BOSS!</b> BossId = '{response.bossId}'</color>");
                    if (gameObject.activeInHierarchy)
                    {
                        StartCoroutine(TriggerBossEncounterFromAi(response.bossId));
                    }
                }
                else
                {
                    Debug.Log("<color=#FFFF00>[StoryPresenter] AI Bedrock không kích hoạt trận đánh ở lượt này (triggerBattle = false).</color>");
                }
            }
            else
            {
                Debug.LogWarning("[StoryPresenter] AI Bedrock không phản hồi, dùng fallback mock action.");
                StoryData nextStoryData = GameProgressService.Instance != null
                    ? GameProgressService.Instance.ExecuteCustomStoryAction(userText)
                    : CreateMockCustomResponse(userText);

                PlayNextStoryNode(nextStoryData);

                if (gameObject.activeInHierarchy)
                {
                    StartCoroutine(TryTriggerBossEncounterDelayed());
                }
            }
        }
    }

    private void PlayNextStoryNode(StoryData nextStoryData)
    {
        if (nextStoryData != null && nextStoryData.node != null && nextStoryData.node.lines != null)
        {
            currentData = nextStoryData;
            pendingLines.Clear();
            for (int index = 0; index < currentData.node.lines.Count; index++)
            {
                pendingLines.Enqueue(currentData.node.lines[index]);
            }

            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
            }
            playbackCoroutine = StartCoroutine(PlayStoryRoutine());
        }
    }

    private IEnumerator TriggerBossEncounterFromAi(string bossId)
    {
        yield return new WaitForSeconds(1.5f);

        if (GameProgressService.Instance == null) yield break;
        GameProgressService.Instance.SpawnRandomBoss();
        var boss = GameProgressService.Instance.CurrentBoss;
        if (boss == null) yield break;

        view.AppendStoryText("\n\n<i><color=#FF3333>AI Bedrock: Một quái vật hùng mạnh bất ngờ xuất hiện!</color></i>\n");
        yield return new WaitForSeconds(1.0f);

        isBossPopupShowing = true;
        view.ShowBossOverlay();
        yield return new WaitForSeconds(1.5f);

        view.ShowBossPanel(
            bossName: boss.name,
            bossRarity: boss.rarity,
            bossLevel: boss.level,
            onFight: OnBossPopupFight,
            onFlee: OnBossPopupFlee
        );
    }

    private StoryData MapActionResponseToStoryData(StoryActionResponse response)
    {
        var characterState = new StoryCharacterState
        {
            characterName = response.character != null ? response.character.name : (GameProgressService.Instance?.CurrentCharacter?.name ?? "Player"),
            level = response.character != null ? response.character.level : (GameProgressService.Instance?.CurrentCharacter?.level ?? 1),
            hp = response.character != null ? response.character.hp : (GameProgressService.Instance?.CurrentCharacter?.hp ?? 100),
            gold = response.character != null ? response.character.gold : (GameProgressService.Instance?.CurrentCharacter?.gold ?? 0)
        };

        var lines = new List<StoryLineData>
        {
            new StoryLineData
            {
                text = response.narrativeText,
                pauseAfter = 0.2f
            }
        };

        var choices = new List<StoryChoiceData>();
        if (response.choices != null)
        {
            foreach (var c in response.choices)
            {
                choices.Add(new StoryChoiceData
                {
                    label = c.label,
                    description = c.description,
                    nextNodeId = c.nextNodeId
                });
            }
        }

        return new StoryData
        {
            title = response.currentLocation ?? "AI Story",
            node = new StoryNodeData
            {
                nodeId = response.currentNodeId ?? "ai_node",
                backgroundKey = string.Empty,
                character = characterState,
                lines = lines,
                choices = choices
            }
        };
    }

    /// <summary>
    /// Đợi story response hiển thị xong rồi mới roll boss encounter.
    /// Delay đủ để người chơi đọc được phản hồi cốt truyện trước.
    /// </summary>
    private IEnumerator TryTriggerBossEncounterDelayed()
    {
        // Đợi người chơi đọc story response
        yield return new WaitForSeconds(2.5f);

        float roll = UnityEngine.Random.value;
        if (roll > bossEncounterChance) yield break;

        // Spawn boss ngẫu nhiên
        if (GameProgressService.Instance == null) yield break;

        GameProgressService.Instance.SpawnRandomBoss();
        var boss = GameProgressService.Instance.CurrentBoss;
        if (boss == null) yield break;

        // ── Bước 1: Cảnh báo nhỏ trong story log ─────────────────────
        view.AppendStoryText(
            $"\n\n<i><color=#FFAA00>Bạn cảm nhận một luồng khí lạnh... Có gì đó đang tiến đến!</color></i>\n"
        );

        yield return new WaitForSeconds(1.0f);

        // ── Bước 2: Overlay con mắt xuất hiện TRƯỚC (full opacity) ───
        isBossPopupShowing = true;
        view.ShowBossOverlay();

        // Đợi người chơi "thấm" hình overlay
        yield return new WaitForSeconds(1.5f);

        // ── Bước 3: Sau đó mới hiện panel thông tin boss ─────────────
        view.ShowBossPanel(
            bossName:   boss.name,
            bossRarity: boss.rarity,
            bossLevel:  boss.level,
            onFight:    OnBossPopupFight,
            onFlee:     OnBossPopupFlee
        );
    }

    private void OnBossPopupFight()
    {
        isBossPopupShowing = false;
        view.HideBossEncounterPopup();
        Debug.Log($"[StoryPresenter] Người chơi chọn Chiến đấu! Boss: {GameProgressService.Instance?.CurrentBoss?.name}");
        SceneManager.LoadScene(battleScene);
    }

    private void OnBossPopupFlee()
    {
        isBossPopupShowing = false;
        view.HideBossEncounterPopup();
        Debug.Log("[StoryPresenter] Người chơi bỏ chạy! Tiếp tục story...");

        // Thông báo nhỏ trong story log
        view.AppendStoryText(
            "\n<i><color=#AAAAAA>Bạn đã bỏ chạy thành công... Nhưng boss vẫn đang rình rập đâu đó.</color></i>\n"
        );

        // Mở lại ô nhập hành động
        awaitingChoice = true;
        view.SetInputPanelVisible(true);
        view.SetInputInteractable(true);
    }

    private void OnBackClicked()
    {
        Debug.Log("[StoryPresenter] Quay về Menu.");
        SceneManager.LoadScene(menuScene);
    }

    private StoryData CreateMockCustomResponse(string userText)
    {
        return new StoryData
        {
            title = "Continuation",
            node = new StoryNodeData
            {
                nodeId = "custom_mock",
                lines = new List<StoryLineData>
                {
                    new StoryLineData
                    {
                        text = $"Hành động của bạn ('{userText}') đã tạo nên bước ngoặt mới trong hầm ngục...",
                        pauseAfter = 0.2f
                    }
                }
            }
        };
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
