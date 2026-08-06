using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.AIStory.Builder;
using GameBackend.Core.AIStory;
using GameBackend.Core.AIStory.DTOs;
using GameBackend.Core.Services.Parsing;
using GameShared.DTOs.Character;
using GameShared.DTOs.Story;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
    public class StoryService : IStoryService
    {
        private const string DefaultLocation = "prologue";
        private const string DefaultChapterId = "introduction";
        private const string DefaultSystemPrompt = "You are a dungeon master for a dark fantasy RPG game. Respond in Vietnamese.";

        private readonly IStoryRepository _storyRepository;
        private readonly ICharacterRepository _characterRepository;
        private readonly IBedrockService _bedrockService;
        private readonly ICharacterService _characterService;
        private readonly IInventoryService _inventoryService;
        private readonly IGamePromptContextBuilder _gamePromptContextBuilder;
        private readonly IStoryStateUpdater _storyStateUpdater;
        private readonly IGameRuleValidator _gameRuleValidator;
        private readonly IPromptBuilder _promptBuilder;
        private readonly IStorySummaryService _storySummaryService;
        private readonly ILogger<StoryService> _logger;

        public StoryService(
            IStoryRepository storyRepository,
            ICharacterRepository characterRepository,
            IBedrockService bedrockService,
            ICharacterService characterService,
            IInventoryService inventoryService,
            IGamePromptContextBuilder gamePromptContextBuilder,
            IStoryStateUpdater storyStateUpdater,
            IGameRuleValidator gameRuleValidator,
            IPromptBuilder promptBuilder,
            IStorySummaryService storySummaryService,
            ILogger<StoryService> logger)
        {
            _storyRepository = storyRepository;
            _characterRepository = characterRepository;
            _bedrockService = bedrockService;
            _characterService = characterService;
            _inventoryService = inventoryService;
            _gamePromptContextBuilder = gamePromptContextBuilder;
            _storyStateUpdater = storyStateUpdater;
            _gameRuleValidator = gameRuleValidator;
            _promptBuilder = promptBuilder;
            _storySummaryService = storySummaryService;
            _logger = logger;
        }

        private static readonly Dictionary<string, List<string>> LocationMobs = new()
        {
            { "ancient_cave", new() { "mob_cave_spider", "mob_goblin_scout" } },
            { "forgotten_temple", new() { "mob_shadow_spirit", "mob_temple_golem" } },
            { "goblin_hideout", new() { "mob_goblin_guard", "mob_goblin_scout" } }
        };

        private readonly Random _randomEncounterGenerator = new();

        private string? RollRandomEncounter(string locationId)
        {
            if (string.IsNullOrWhiteSpace(locationId)) return null;

            // 35% chance of random encounter
            if (_randomEncounterGenerator.NextDouble() > 0.35) return null;

            var normalizedLocation = locationId.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");
            if (LocationMobs.TryGetValue(normalizedLocation, out var mobs) && mobs.Count > 0)
            {
                return mobs[_randomEncounterGenerator.Next(mobs.Count)];
            }

            return null;
        }

        public async Task<StoryActionResponse> StartStoryAsync(StoryStartRequest request)
        {
            var character = await _characterRepository.GetByIdAsync(request.characterId)
                ?? throw new Utils.GameNotFoundException("Character not found");

            var existingSession = await _storyRepository.GetSessionByCharacterIdAsync(character.characterId);
            if (existingSession != null && existingSession.status == "Active")
            {
                var allRecentActions = await _storyRepository.GetActionsBySessionIdAsync(existingSession.sessionId);
                var latestAction = allRecentActions.OrderByDescending(a => a.createdAt).FirstOrDefault();
                var lastNarrative = latestAction?.aiResponse ?? existingSession.storySummary ?? string.Empty;
                List<StoryChoiceOption>? lastChoices = null;
                if (latestAction != null)
                {
                    try
                    {
                        var parsed = GameBackend.Core.Services.Parsing.StoryAiResponseParser.Parse(latestAction.aiResponse, existingSession, "choice");
                        lastNarrative = parsed.NarrativeText;
                        lastChoices = parsed.Choices;
                    }
                    catch { }
                }

                return BuildResponse(existingSession, character, lastNarrative, lastChoices);
            }

            var session = new StorySession
            {
                sessionId = Guid.NewGuid().ToString("N"),
                characterId = character.characterId,
                currentLocation = DefaultLocation,
                currentChapterId = string.IsNullOrWhiteSpace(request.storyFileId) ? DefaultChapterId : request.storyFileId,
                currentNodeId = DefaultChapterId,
                status = "Active",
                updatedAt = DateTime.UtcNow,
                storyVersion = string.IsNullOrWhiteSpace(request.storyFileId) ? "1.0" : request.storyFileId,
                storySummary = "Mở đầu cuộc phiêu lưu tại tàn tích cổ.",
                sourceType = "AI"
            };

            // Sync character's starting location with the story session location
            character.currentLocationId = session.currentLocation;

            var openingContext = new StoryActionProcessingContext
            {
                Character = character,
                Session = session,
                PlayerInput = string.Empty,
                RecentActions = new List<StoryAction>(),
                PromptContext = await _gamePromptContextBuilder.BuildAsync(character, new List<Item>(), new List<StoryAction>(), session, string.Empty)
            };

            var openingResponse = await GenerateStoryAiResponseAsync(openingContext, "opening");
            openingResponse = await _gameRuleValidator.ValidateAndSanitizeAsync(session, character, openingResponse);
            await _storyStateUpdater.ApplyAsync(session, character, openingResponse);
            _logger.LogInformation("Story session started: {SessionId} for character: {CharacterId}", session.sessionId, character.characterId);

            return BuildResponse(session, character, openingResponse.NarrativeText, openingResponse.Choices);
        }

        public async Task<StoryActionResponse> ProcessActionAsync(StoryActionRequest request)
        {
            // Mục 6: Kiểm tra nhân vật còn sống không (tự động hồi sinh nếu đủ thời gian)
            await _characterService.EnsureAliveOrAutoReviveAsync(request.characterId);

            var character = await _characterRepository.GetByIdAsync(request.characterId)
                ?? throw new Utils.GameNotFoundException("Character not found");

            var session = await _storyRepository.GetSessionByCharacterIdAsync(request.characterId)
                ?? throw new Utils.GameNotFoundException("Active session not found");

            if (session.sessionId != request.sessionId)
            {
                throw new Utils.GameNotFoundException("Session mismatch");
            }

            string? systemInjectedEvent = null;
            if (session.currentNodeId != "boss_room" && session.status == "Active" && request.choiceIndex != 2) // Do not ambush during rest
            {
                var mobId = RollRandomEncounter(session.currentLocation);
                if (mobId != null)
                {
                    var mob = GameShared.Config.GameConstants.BossCatalog.FirstOrDefault(b => b.bossId == mobId);
                    if (mob != null)
                    {
                        systemInjectedEvent = $"[SỰ KIỆN QUÁI VẬT] Một con {mob.name} xuất hiện đột ngột cản đường bạn! Trận chiến bắt đầu! Bạn phải mô tả cuộc chạm trán này trong narrativeText, đồng thời bắt buộc đặt triggerBattle: true, bossId: '{mobId}', bossName: '{mob.name}', bossLevel: {character.level}.";
                    }
                }
            }

            // If player provided free-form input, run AI-driven orchestration
            if (!string.IsNullOrWhiteSpace(request.playerInput))
            {
                return await ProcessFreeFormActionAsync(request, character, session, systemInjectedEvent);
            }

            return await ProcessChoiceActionAsync(request, character, session, systemInjectedEvent);
        }

        private async Task<StoryActionResponse> ProcessFreeFormActionAsync(
            StoryActionRequest request,
            Character character,
            StorySession session,
            string? systemInjectedEvent)
        {
            var context = await LoadGameContextAsync(request, character, session, systemInjectedEvent);
            var aiResponse = await GenerateStoryAiResponseAsync(context, "player_action");

            if (string.IsNullOrWhiteSpace(aiResponse.NarrativeText))
            {
                aiResponse = CreateFallbackResponse(context, "Bạn tiếp tục cuộc phiêu lưu trong bóng tối...");
            }

            aiResponse = await _gameRuleValidator.ValidateAndSanitizeAsync(session, character, aiResponse);

            await _storyStateUpdater.ApplyAsync(session, character, aiResponse);

            await SaveStoryTurnAsync(context, aiResponse);

            return BuildResponse(context, aiResponse.NarrativeText, aiResponse);
        }

        private async Task<StoryActionResponse> ProcessChoiceActionAsync(
            StoryActionRequest request,
            Character character,
            StorySession session,
            string? systemInjectedEvent)
        {
            var context = await LoadGameContextAsync(request, character, session, systemInjectedEvent);
            var aiResponse = await GenerateStoryAiResponseAsync(context, "choice");

            if (string.IsNullOrWhiteSpace(aiResponse.NarrativeText))
            {
                aiResponse = CreateFallbackResponseFromChoice(request.choiceIndex, context);
            }

            aiResponse = await _gameRuleValidator.ValidateAndSanitizeAsync(session, character, aiResponse);

            await _storyStateUpdater.ApplyAsync(session, character, aiResponse);

            await SaveStoryTurnAsync(context, aiResponse, "choice");

            return BuildResponse(context, aiResponse.NarrativeText, aiResponse);
        }

        private async Task<StoryActionProcessingContext> LoadGameContextAsync(
            StoryActionRequest request,
            Character character,
            StorySession session,
            string? systemInjectedEvent = null)
        {
            var inventoryResponse = await _inventoryService.GetInventoryAsync(character.characterId);
            var inventoryItems = (inventoryResponse?.slots ?? new List<GameShared.DTOs.Inventory.InventorySlot>())
                .Select(slot => new Item
                {
                    itemId = slot.itemId,
                    name = slot.itemId,
                    rarity = "Common"
                })
                .ToList();

            var allRecentActions = await _storyRepository.GetActionsBySessionIdAsync(session.sessionId);
            var recentActions = allRecentActions
                .OrderByDescending(action => action.createdAt)
                .Take(6)
                .OrderBy(action => action.createdAt)
                .ToList();

            var promptContext = await _gamePromptContextBuilder.BuildAsync(
                character,
                inventoryItems,
                recentActions,
                session,
                request.playerInput,
                systemInjectedEvent);

            return new StoryActionProcessingContext
            {
                Character = character,
                Session = session,
                PlayerInput = request.playerInput,
                RecentActions = allRecentActions,
                PromptContext = promptContext
            };
        }

        private async Task<StoryAiResponse> GenerateStoryAiResponseAsync(StoryActionProcessingContext context, string defaultActionType)
        {
            var prompt = _promptBuilder.Build(context.PromptContext);
            prompt += "\n\nReturn ONLY a valid JSON object matching the following schema. Do NOT wrap in markdown code blocks like ```json, and do NOT include any extra text:\n" +
                      "{\n" +
                      "  \"narrativeText\": \"Vietnamese story response description text here\",\n" +
                      "  \"currentNodeId\": \"current node ID\",\n" +
                      "  \"currentLocation\": \"current location ID\",\n" +
                      "  \"currentChapterId\": \"current chapter ID\",\n" +
                      "  \"storySummary\": \"brief updated summary of the story so far\",\n" +
                      "  \"actionType\": \"player_action\",\n" +
                      "  \"triggerBattle\": false,\n" +
                      "  \"bossId\": \"\",\n" +
                      "  \"bossName\": \"\",\n" +
                      "  \"bossLevel\": null,\n" +
                      "  \"characterDelta\": {\n" +
                      "    \"hpDelta\": 0,\n" +
                      "    \"goldDelta\": 0,\n" +
                      "    \"expDelta\": 0,\n" +
                      "    \"mpDelta\": 0,\n" +
                      "    \"status\": \"Alive\",\n" +
                      "    \"currentLocationId\": \"location ID\"\n" +
                      "  },\n" +
                      "  \"inventoryChanges\": [],\n" +
                      "  \"choices\": [\n" +
                      "    { \"label\": \"Choice Label\", \"description\": \"Choice Description\", \"nextNodeId\": \"next_node_id\" }\n" +
                      "  ]\n" +
                      "}";

            var rawResponse = await GenerateRawAiResponseAsync(DefaultSystemPrompt, prompt, context.Session.storySummary ?? string.Empty);
            return StoryAiResponseParser.Parse(rawResponse, context.Session, defaultActionType, _logger);
        }

        private async Task SaveStoryTurnAsync(StoryActionProcessingContext context, StoryAiResponse aiResponse, string defaultActionType = "player_action")
        {
            var turnNumber = (context.RecentActions?.Count ?? 0) + 1;
            var action = new StoryAction
            {
                actionId = Guid.NewGuid().ToString("N"),
                sessionId = context.Session.sessionId,
                playerInput = context.PlayerInput,
                aiResponse = aiResponse.NarrativeText,
                turnNumber = turnNumber,
                actionType = aiResponse.ActionType ?? defaultActionType,
                metadataJson = StoryAiResponseParser.Serialize(aiResponse),
                createdAt = DateTime.UtcNow
            };

            await _storyRepository.SaveActionAsync(action);

            var oldSummary = context.Session.storySummary;
            var newSummary = await _storySummaryService.CondenseSummaryIfNeededAsync(context.Session, turnNumber, context.RecentActions);
            if (oldSummary != newSummary)
            {
                await _storyRepository.SaveSessionAsync(context.Session);
            }
        }

        private static StoryActionResponse BuildResponse(StoryActionProcessingContext context, string narrativeText, StoryAiResponse aiResponse)
        {
            var response = BuildResponse(context.Session, context.Character, narrativeText, aiResponse.Choices);
            response.triggerBattle = aiResponse.TriggerBattle;
            response.bossId = aiResponse.BossId;
            return response;
        }

        private async Task<string> GenerateRawAiResponseAsync(string systemPrompt, string userPrompt, string fallbackNarrative)
        {
            try
            {
                if (await _bedrockService.IsAvailableAsync())
                {
                    var raw = await _bedrockService.GenerateNarrativeAsync(systemPrompt, userPrompt);
                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        return raw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bedrock AI fallback triggered for story prompt");
            }

            return fallbackNarrative;
        }

        private StoryAiResponse CreateFallbackResponse(StoryActionProcessingContext context, string rawResponse)
        {
            return new StoryAiResponse
            {
                NarrativeText = rawResponse,
                CurrentNodeId = context.Session.currentNodeId,
                CurrentLocation = context.Session.currentLocation,
                CurrentChapterId = context.Session.currentChapterId,
                StorySummary = context.Session.storySummary,
                ActionType = "player_action",
                MetadataJson = "{}",
                CharacterDelta = new StoryAiCharacterDelta()
            };
        }

        private StoryAiResponse CreateFallbackResponseFromChoice(int choiceIndex, StoryActionProcessingContext context)
        {
            return choiceIndex switch
            {
                0 => new StoryAiResponse
                {
                    NarrativeText = "Bạn giơ vũ khí lên và xông thẳng vào sương mù!",
                    CurrentNodeId = "battle_path",
                    CurrentLocation = context.Session.currentLocation,
                    CurrentChapterId = context.Session.currentChapterId,
                    StorySummary = context.Session.storySummary,
                    ActionType = "choice",
                    MetadataJson = "{}",
                    TriggerBattle = true,
                    BossId = "boss_goblin_chief",
                    BossLevel = 10,
                    CharacterDelta = new StoryAiCharacterDelta { ExpDelta = 25 }
                },
                1 => new StoryAiResponse
                {
                    NarrativeText = "Bạn kiểm tra các ký tự kỳ lạ trên sàn đá, phát hiện ngăn bí mật chứa vàng cổ!",
                    CurrentNodeId = "investigate_path",
                    CurrentLocation = context.Session.currentLocation,
                    CurrentChapterId = context.Session.currentChapterId,
                    StorySummary = context.Session.storySummary,
                    ActionType = "choice",
                    MetadataJson = "{}",
                    TriggerBattle = false,
                    CharacterDelta = new StoryAiCharacterDelta { GoldDelta = 15, ExpDelta = 15 }
                },
                _ => new StoryAiResponse
                {
                    NarrativeText = "Bạn đốt lửa nghỉ ngơi. Sức khỏe phục hồi nhưng tốn 5 vàng mua lương khô.",
                    CurrentNodeId = "rest_path",
                    CurrentLocation = context.Session.currentLocation,
                    CurrentChapterId = context.Session.currentChapterId,
                    StorySummary = context.Session.storySummary,
                    ActionType = "choice",
                    MetadataJson = "{}",
                    TriggerBattle = false,
                    CharacterDelta = new StoryAiCharacterDelta { HpDelta = 18, GoldDelta = -5, ExpDelta = 5 }
                }
            };
        }

        private static StoryActionResponse BuildResponse(StorySession session, Character character, string narrativeText, List<StoryChoiceOption>? aiChoices = null)
        {
            return new StoryActionResponse
            {
                sessionId = session.sessionId,
                currentNodeId = session.currentNodeId,
                currentLocation = session.currentLocation,
                narrativeText = narrativeText,
                character = new CharacterResponse
                {
                    characterId = character.characterId,
                    name = character.name,
                    level = character.level,
                    experience = character.experience,
                    hp = character.hp,
                    maxHp = character.maxHp,
                    attack = character.attack,
                    defense = character.defense,
                    criticalRate = character.criticalRate,
                    luckyRate = character.luckyRate,
                    gold = character.gold,
                    className = character.className,
                    status = character.status,
                    currentLocationId = character.currentLocationId
                },
                choices = (aiChoices != null && aiChoices.Count > 0)
                    ? aiChoices
                    : new List<StoryChoiceOption>
                    {
                        new() { label = "Tấn công", description = "Chiến đấu với Boss quái vật", nextNodeId = "battle_path" },
                        new() { label = "Điều tra", description = "Tìm kiếm lối đi bí ẩn", nextNodeId = "investigate_path" },
                        new() { label = "Nghỉ ngơi", description = "Hồi phục sức khỏe", nextNodeId = "rest_path" }
                    },
                triggerBattle = false
            };
        }

        private sealed class StoryActionProcessingContext
        {
            public required Character Character { get; init; }

            public required StorySession Session { get; init; }

            public required string PlayerInput { get; init; }

            public List<StoryAction> RecentActions { get; init; } = new();

            public required GamePromptContext PromptContext { get; init; }
        }
    }
}
