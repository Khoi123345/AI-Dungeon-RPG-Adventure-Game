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
    private bool isExternalModalOpen;
    private bool isInputMode = false;
    private bool isActionRequestInFlight;
    private bool isDestroyed;

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
            view.BindSwitchModes(HandleSwitchToInput, HandleSwitchToOptions);
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

    private void OnEnable()
    {
        if (GameProgressService.Instance != null)
        {
            GameProgressService.Instance.OnCharacterStatsChanged += OnCharacterStatsUpdated;
        }
    }

    private void OnDisable()
    {
        if (GameProgressService.Instance != null)
        {
            GameProgressService.Instance.OnCharacterStatsChanged -= OnCharacterStatsUpdated;
        }
    }

    private void OnDestroy()
    {
        isDestroyed = true;
        isActionRequestInFlight = false;
        playbackCoroutine = null;
    }

    private void OnCharacterStatsUpdated(GameShared.Models.Character curChar)
    {
        if (view != null && curChar != null)
        {
            view.SetCharacterState(new StoryCharacterState
            {
                characterName = curChar.name,
                level = curChar.level,
                hp = curChar.hp,
                gold = curChar.gold,
                xp = curChar.experience,
                maxXP = curChar.level * 100
            });
        }
    }

    private bool IsMockMode => GameConfigSO.Instance != null ? GameConfigSO.Instance.useMockMode : useMockStoryOnStart;

    public async void StartRealStory()
    {
        if (view != null)
        {
            view.SetStoryText("<i><color=#AAAAAA>AI đang tạo cốt truyện...</color></i>");
            view.SetNextIndicatorVisible(false);
            view.SetActiveOptionsPanelVisible(false);
            view.SetInputPanelVisible(false);

            if (GameProgressService.Instance?.CurrentCharacter != null)
            {
                var curChar = GameProgressService.Instance.CurrentCharacter;
                view.SetCharacterState(new StoryCharacterState
                {
                    characterName = curChar.name,
                    level = curChar.level,
                    hp = curChar.hp,
                    gold = curChar.gold,
                    xp = curChar.experience,
                    maxXP = curChar.level * 100
                });
            }
        }

        string characterId = GameProgressService.Instance?.CurrentCharacter?.characterId;
        if (string.IsNullOrEmpty(characterId))
        {
            characterId = PlayerPrefs.GetString("lastCharacterId", "demo_char_id");
        }


        Debug.Log($"[StoryPresenter] Gửi yêu cầu /story/start với characterId={characterId}");
        var response = await storyApiService.StartStoryAsync(characterId, "prologue");

        if (this == null || isDestroyed) return;

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
        view.SetActiveOptionsPanelVisible(false);
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
        bool hasChoices = currentData != null && currentData.node != null && currentData.node.choices != null && currentData.node.choices.Count > 0;

        if (currentData != null && currentData.isCompleted)
        {
            awaitingChoice = false;
            view.SetActiveOptionsPanelVisible(false);
            view.SetInputPanelVisible(false);
            view.SetChoiceInteractable(false);
            view.SetInputInteractable(false);
            yield break;
        }
        
        if (hasChoices)
        {
            view.SetChoices(currentData.node.choices.ToArray(), OnChoiceSelected);
        }

        if (isExternalModalOpen)
        {
            view.SetActiveOptionsPanelVisible(false);
            view.SetInputPanelVisible(false);
        }
        else if (hasChoices && !isInputMode)
        {
            view.SetActiveOptionsPanelVisible(true);
            view.SetInputPanelVisible(false);
        }
        else
        {
            isInputMode = true;
            view.SetActiveOptionsPanelVisible(false);
            view.SetInputPanelVisible(true);
        }

        view.SetChoiceInteractable(true);
        view.SetInputInteractable(true);
        awaitingChoice = true;
    }

    private void HandleSwitchToInput()
    {
        isInputMode = true;
        view.SetActiveOptionsPanelVisible(false);
        view.SetInputPanelVisible(true);
    }

    private void HandleSwitchToOptions()
    {
        isInputMode = false;
        view.SetInputPanelVisible(false);
        view.SetActiveOptionsPanelVisible(true);
    }

    private IEnumerator TypeLineRoutine(string text)
    {
        isTyping = true;
        skipTyping = false;
        waitingForAdvance = false;

        try
        {
            if (string.IsNullOrEmpty(text))
            {
                yield break;
            }

            string renderedText = string.Empty;
            int characterIndex = 0;

            while (characterIndex < text.Length)
            {
                if (skipTyping)
                {
                    renderedText = text;
                    break;
                }

                renderedText += text[characterIndex];
                view?.SetStoryText(renderedText);
                characterIndex++;
                yield return new WaitForSeconds(characterDelay);
            }

            view?.SetStoryText(renderedText);

            isTyping = false;
            waitingForAdvance = true;
            view?.SetNextIndicatorVisible(true);

            while (waitingForAdvance)
            {
                yield return null;
            }
        }
        finally
        {
            isTyping = false;
            waitingForAdvance = false;
            view?.SetNextIndicatorVisible(false);
        }
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
        if (this == null || isDestroyed || isActionRequestInFlight || !awaitingChoice)
        {
            return;
        }

        if (currentData == null || currentData.node == null || currentData.node.choices == null)
        {
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= currentData.node.choices.Count)
        {
            return;
        }

        StoryChoiceData choice = currentData.node.choices[choiceIndex];
        isActionRequestInFlight = true;

        int cost = GameShared.Config.GameConstants.StoryCostPerTurn;
        var character = GameProgressService.Instance?.CurrentCharacter;
        if (character != null)
        {
            if (character.gold < cost)
            {
                view.AppendStoryText($"\n\n<b><color=#FF4444>⚠️ Bạn không đủ Vàng! Mỗi lượt AI kể chuyện yêu cầu {cost} Gold. (Hiện có: {character.gold} Gold). Hãy chiến đấu đánh quái/Boss để kiếm thêm Vàng!</color></b>\n\n");
                isActionRequestInFlight = false;
                return;
            }
            character.gold -= cost;
            view.SetCharacterState(new StoryCharacterState
            {
                characterName = character.name,
                level = character.level,
                hp = character.hp,
                gold = character.gold,
                xp = character.experience,
                maxXP = character.level * 100
            });
            Debug.Log($"[StoryPresenter] Đã trừ trực tiếp {cost} Gold trên client (Choice). Vàng còn lại: {character.gold}");
        }

        awaitingChoice = false;
        view.SetActiveOptionsPanelVisible(false);
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
            view.AppendStoryText("<i><color=#888888>[AI đang tạo cốt truyện...]</color></i>\n");

            string characterId = GameProgressService.Instance?.CurrentCharacter?.characterId ?? "demo_char_id";
            string sessionId = GameProgressService.Instance?.CurrentStorySession?.sessionId ?? "";

            var response = await storyApiService.SendActionAsync(characterId, sessionId, choiceIndex, choice.label);

            if (this == null || isDestroyed) return;
            isActionRequestInFlight = false;

            if (response != null && !string.IsNullOrEmpty(response.narrativeText))
            {
                Debug.Log($"<color=#00FF00>[StoryPresenter] Nhận phản hồi từ AI Bedrock (Choice):</color>\n- triggerBattle: <b>{response.triggerBattle}</b>\n- bossId: <b>{response.bossId}</b>\n- location: <b>{response.currentLocation}</b>");

                if (response.character != null)
                {
                    GameProgressService.Instance?.SyncCharacterFromResponse(response.character);
                }

                GameProgressService.Instance?.SetCurrentStorySession(response.sessionId, response.currentNodeId, response.currentLocation);
                StoryData nextStoryData = MapActionResponseToStoryData(response);
                PlayNextStoryNode(nextStoryData);

                if (response.triggerBattle)
                {
                    Debug.Log($"<color=#FF5500><b>[StoryPresenter] AI CHÍNH THỨC KÍCH HOẠT TRẬN ĐÁNH BOSS!</b> BossId = '{response.bossId}'</color>");
                    view?.SetActiveOptionsPanelVisible(false);
                    if (gameObject.activeInHierarchy)
                    {
                        StartCoroutine(TriggerBossEncounterFromAi(response.bossId, response.bossLevel));
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
                RestoreInteractionUiAfterModal();
            }
        }

        isActionRequestInFlight = false;
    }

    private async void HandleUserActionSubmitted(string userText)
    {
        if (this == null || isDestroyed || isActionRequestInFlight || string.IsNullOrWhiteSpace(userText) || !awaitingChoice)
        {
            return;
        }

        isActionRequestInFlight = true;

        int cost = GameShared.Config.GameConstants.StoryCostPerTurn;
        var character = GameProgressService.Instance?.CurrentCharacter;
        if (character != null)
        {
            if (character.gold < cost)
            {
                view.AppendStoryText($"\n\n<b><color=#FF4444>⚠️ Bạn không đủ Vàng! Mỗi lượt AI kể chuyện yêu cầu {cost} Gold. (Hiện có: {character.gold} Gold). Hãy chiến đấu đánh quái/Boss để kiếm thêm Vàng!</color></b>\n\n");
                isActionRequestInFlight = false;
                return;
            }
            character.gold -= cost;
            view.SetCharacterState(new StoryCharacterState
            {
                characterName = character.name,
                level = character.level,
                hp = character.hp,
                gold = character.gold,
                xp = character.experience,
                maxXP = character.level * 100
            });
            Debug.Log($"[StoryPresenter] Đã trừ trực tiếp {cost} Gold trên client. Vàng còn lại: {character.gold}");
        }

        awaitingChoice = false;
        view.SetActiveOptionsPanelVisible(false);
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
            view.AppendStoryText("<i><color=#888888>[AI đang tạo cốt truyện...]</color></i>\n");

            string characterId = GameProgressService.Instance?.CurrentCharacter?.characterId ?? "demo_char_id";
            string sessionId = GameProgressService.Instance?.CurrentStorySession?.sessionId ?? "";

            var response = await storyApiService.SendActionAsync(characterId, sessionId, choiceIndex: -1, playerInput: userText);

            if (this == null || isDestroyed) return;
            isActionRequestInFlight = false;

            if (response != null && !string.IsNullOrEmpty(response.narrativeText))
            {
                Debug.Log($"<color=#00FF00>[StoryPresenter] Nhận phản hồi từ AI Bedrock:</color>\n- triggerBattle: <b>{response.triggerBattle}</b>\n- bossId: <b>{response.bossId}</b>\n- location: <b>{response.currentLocation}</b>");

                if (response.character != null)
                {
                    GameProgressService.Instance?.SyncCharacterFromResponse(response.character);
                }

                GameProgressService.Instance?.SetCurrentStorySession(response.sessionId, response.currentNodeId, response.currentLocation);
                StoryData nextStoryData = MapActionResponseToStoryData(response);
                PlayNextStoryNode(nextStoryData);


                if (response.triggerBattle)
                {
                    Debug.Log($"<color=#FF5500><b>[StoryPresenter] AI CHÍNH THỨC KÍCH HOẠT TRẬN ĐÁNH BOSS!</b> BossId = '{response.bossId}'</color>");
                    if (gameObject.activeInHierarchy)
                    {
                        StartCoroutine(TriggerBossEncounterFromAi(response.bossId, response.bossLevel));
                    }

                }
                else
                {
                    Debug.Log("<color=#FFFF00>[StoryPresenter] AI Bedrock không kích hoạt trận đánh ở lượt này (triggerBattle = false).</color>");
                }
            }
            else
            {
                Debug.LogWarning("[StoryPresenter] Story API không phản hồi. Không tạo encounter mock trong Online Mode.");
                view?.AppendStoryText("\n<i><color=#FF6666>Không thể tiếp tục hành động lúc này. Vui lòng thử lại.</color></i>\n");
                RestoreInteractionUiAfterModal();
            }
        }

        isActionRequestInFlight = false;
    }

    private void PlayNextStoryNode(StoryData nextStoryData)
    {
        if (this == null || isDestroyed || !isActiveAndEnabled) return;

        if (nextStoryData != null && nextStoryData.node != null && nextStoryData.node.lines != null)
        {
            currentData = nextStoryData;
            if (currentData.node.character != null)
            {
                view.SetCharacterState(currentData.node.character);
            }

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

    private IEnumerator TriggerBossEncounterFromAi(string bossId, int? bossLevel = null)
    {
        // 1. Chờ cốt truyện hiển thị xong hoàn toàn (chữ gõ xong + người chơi bấm tiếp tục)
        if (playbackCoroutine != null)
        {
            yield return playbackCoroutine;
        }

        // 2. Tạm dừng một chút để người chơi đọc/thấm không khí trước khi quái xuất hiện
        yield return new WaitForSeconds(1.5f);

        if (GameProgressService.Instance == null) yield break;
        if (!GameProgressService.Instance.SpawnBossById(bossId, bossLevel))
        {
            Debug.LogError($"[StoryPresenter] Hủy encounter vì bossId '{bossId}' rỗng hoặc không tồn tại trong BossCatalog.");
            view?.AppendStoryText("\n<i><color=#FF6666>Không thể xác định kẻ địch cho trận đấu này. Hãy chọn một hành động khác.</color></i>\n");
            ExitEncounterModal();
            RestoreInteractionUiAfterModal();
            yield break;
        }

        var boss = GameProgressService.Instance.CurrentBoss;
        if (boss == null) yield break;

        view.AppendStoryText("\n\n<i><color=#FF3333>AI Bedrock: Một quái vật hùng mạnh bất ngờ xuất hiện!</color></i>\n");
        yield return new WaitForSeconds(1.0f);

        EnterEncounterModal();
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
        if (response != null && !string.IsNullOrEmpty(response.debugPrompt))
        {
            Debug.Log($"<color=#00FFFF>================ [FULL AI PROMPT SENT TO BEDROCK] ================\n{response.debugPrompt}\n===================================================================</color>");
        }

        string charName = response?.character != null ? response.character.name : (GameProgressService.Instance?.CurrentCharacter?.name ?? "Player");
        int charGold = GameProgressService.Instance?.CurrentCharacter != null 
            ? GameProgressService.Instance.CurrentCharacter.gold 
            : (response?.character != null ? response.character.gold : 0);

        int charLevel = response?.character != null ? response.character.level : (GameProgressService.Instance?.CurrentCharacter?.level ?? 1);
        int charXP = response?.character != null ? response.character.experience : (GameProgressService.Instance?.CurrentCharacter?.experience ?? 0);
        int charMaxXP = charLevel * 100;
        
        var characterState = new StoryCharacterState
        {
            characterName = charName,
            level = charLevel,
            hp = response?.character != null ? response.character.hp : (GameProgressService.Instance?.CurrentCharacter?.hp ?? 100),
            gold = charGold,
            xp = charXP,
            maxXP = charMaxXP
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
            isCompleted = response.storyCompleted,
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
        // 1. Chờ cốt truyện hiển thị xong hoàn toàn
        if (playbackCoroutine != null)
        {
            yield return playbackCoroutine;
        }

        // Đợi người chơi đọc story response
        yield return new WaitForSeconds(1.5f);

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
        EnterEncounterModal();
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
        var boss = GameProgressService.Instance?.CurrentBoss;
        if (boss == null || string.IsNullOrWhiteSpace(boss.bossId))
        {
            Debug.LogError("[StoryPresenter] Không chuyển BattleScene vì CurrentBoss hoặc bossId bị thiếu.");
            ExitEncounterModal();
            RestoreInteractionUiAfterModal();
            return;
        }

        isBossPopupShowing = false;
        view.HideBossEncounterPopup();
        Debug.Log($"[StoryPresenter] Người chơi chọn Chiến đấu! Boss: {boss.name} (bossId={boss.bossId})");
        SceneManager.LoadScene(battleScene);
    }

    private void OnBossPopupFlee()
    {
        ExitEncounterModal();
        Debug.Log("[StoryPresenter] Người chơi bỏ chạy! Tiếp tục story...");

        // Thông báo nhỏ trong story log
        view?.AppendStoryText(
            "\n<i><color=#AAAAAA>Bạn đã bỏ chạy thành công... Nhưng quái vật vẫn đang rình rập đâu đó.</color></i>\n"
        );

        RestoreInteractionUiAfterModal();
    }

    private void EnterEncounterModal()
    {
        isBossPopupShowing = true;
        view?.SetActiveOptionsPanelVisible(false);
        view?.SetInputPanelVisible(false);
        view?.SetChoiceInteractable(false);
        view?.SetInputInteractable(false);
    }

    private void ExitEncounterModal()
    {
        isBossPopupShowing = false;
        view?.HideBossEncounterPopup();
    }

    public void SetExternalModalOpen(bool open)
    {
        isExternalModalOpen = open;
        if (open)
        {
            view?.SetActiveOptionsPanelVisible(false);
            view?.SetInputPanelVisible(false);
            view?.SetChoiceInteractable(false);
            view?.SetInputInteractable(false);
            return;
        }

        RestoreInteractionUiAfterModal();
    }

    public void RestoreInteractionUiAfterModal()
    {
        if (view == null || isExternalModalOpen || isBossPopupShowing) return;
        if (currentData == null || currentData.node == null || currentData.isCompleted)
        {
            awaitingChoice = false;
            view.SetActiveOptionsPanelVisible(false);
            view.SetInputPanelVisible(false);
            return;
        }

        if (isTyping || waitingForAdvance)
        {
            view.SetActiveOptionsPanelVisible(false);
            view.SetInputPanelVisible(false);
            return;
        }

        awaitingChoice = true;
        bool hasChoices = currentData.node.choices != null && currentData.node.choices.Count > 0;
        if (hasChoices && !isInputMode)
        {
            view.SetActiveOptionsPanelVisible(true);
            view.SetInputPanelVisible(false);
            view.SetChoiceInteractable(true);
        }
        else
        {
            isInputMode = true;
            view.SetActiveOptionsPanelVisible(false);
            view.SetInputPanelVisible(true);
            view.SetInputInteractable(true);
        }
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
                gold = 120,
                xp = 60,
                maxXP = 700
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
