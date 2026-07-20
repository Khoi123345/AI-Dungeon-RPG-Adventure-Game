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
        private const string DefaultLocation = "Ancient Ruins";
        private const string DefaultChapterId = "intro";
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
            _logger = logger;
        }

        public async Task<StoryActionResponse> StartStoryAsync(StoryStartRequest request)
        {
            var character = await _characterRepository.GetByIdAsync(request.characterId)
                ?? throw new Utils.GameNotFoundException("Character not found");

            var existingSession = await _storyRepository.GetSessionByCharacterIdAsync(character.characterId);
            if (existingSession != null && existingSession.status == "Active")
            {
                return BuildResponse(existingSession, character, existingSession.storySummary ?? string.Empty);
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

            return BuildResponse(session, character, openingResponse.NarrativeText);
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

            // If player provided free-form input, run AI-driven orchestration
            if (!string.IsNullOrWhiteSpace(request.playerInput))
            {
                return await ProcessFreeFormActionAsync(request, character, session);
            }

            return await ProcessChoiceActionAsync(request, character, session);
        }

        private async Task<StoryActionResponse> ProcessFreeFormActionAsync(
            StoryActionRequest request,
            Character character,
            StorySession session)
        {
            var context = await LoadGameContextAsync(request, character, session);
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
            StorySession session)
        {
            var context = await LoadGameContextAsync(request, character, session);
            var aiResponse = await GenerateStoryAiResponseAsync(context, "choice");

            if (string.IsNullOrWhiteSpace(aiResponse.NarrativeText))
            {
                aiResponse = CreateFallbackResponseFromChoice(request.choiceIndex, context);
            }

            aiResponse = await _gameRuleValidator.ValidateAndSanitizeAsync(session, character, aiResponse);

            await _storyStateUpdater.ApplyAsync(session, character, aiResponse);

            var action = new StoryAction
            {
                actionId = Guid.NewGuid().ToString("N"),
                sessionId = session.sessionId,
                playerInput = request.playerInput,
                aiResponse = aiResponse.NarrativeText,
                turnNumber = 0,
                actionType = aiResponse.ActionType ?? "choice",
                metadataJson = StoryAiResponseParser.Serialize(aiResponse),
                createdAt = DateTime.UtcNow
            };
            await _storyRepository.SaveActionAsync(action);

            return BuildResponse(context, aiResponse.NarrativeText, aiResponse);
        }

        private async Task<StoryActionProcessingContext> LoadGameContextAsync(
            StoryActionRequest request,
            Character character,
            StorySession session)
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
                request.playerInput);

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
            prompt += "\n\nReturn ONLY valid JSON for StoryAiResponse. No markdown, no code fences, no extra commentary. Use camelCase property names.";

            var rawResponse = await GenerateRawAiResponseAsync(DefaultSystemPrompt, prompt, context.Session.storySummary ?? string.Empty);
            return StoryAiResponseParser.Parse(rawResponse, context.Session, defaultActionType, _logger);
        }

        private async Task SaveStoryTurnAsync(StoryActionProcessingContext context, StoryAiResponse aiResponse)
        {
            var action = new StoryAction
            {
                actionId = Guid.NewGuid().ToString("N"),
                sessionId = context.Session.sessionId,
                playerInput = context.PlayerInput,
                aiResponse = aiResponse.NarrativeText,
                turnNumber = (context.RecentActions?.Count ?? 0) + 1,
                actionType = aiResponse.ActionType ?? "player_action",
                metadataJson = StoryAiResponseParser.Serialize(aiResponse),
                createdAt = DateTime.UtcNow
            };

            await _storyRepository.SaveActionAsync(action);
        }

        private static StoryActionResponse BuildResponse(StoryActionProcessingContext context, string narrativeText, StoryAiResponse aiResponse)
        {
            var response = BuildResponse(context.Session, context.Character, narrativeText);
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

        private static StoryActionResponse BuildResponse(StorySession session, Character character, string narrativeText)
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
                    hp = character.hp,
                    maxHp = character.maxHp,
                    gold = character.gold
                },
                choices = new List<StoryChoiceOption>
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
            public Character Character { get; init; }

            public StorySession Session { get; init; }

            public string PlayerInput { get; init; }

            public List<StoryAction> RecentActions { get; init; } = new();

            public GamePromptContext PromptContext { get; init; }
        }
    }
}
