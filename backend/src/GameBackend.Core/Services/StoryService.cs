using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.AIStory.Builder;
using GameBackend.Core.AIStory;
using GameBackend.Core.AIStory.DTOs;
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
        private readonly IStoryContextBuilder _storyContextBuilder;
        private readonly IPromptBuilder _promptBuilder;
        private readonly ILogger<StoryService> _logger;

        public StoryService(
            IStoryRepository storyRepository,
            ICharacterRepository characterRepository,
            IBedrockService bedrockService,
            ICharacterService characterService,
            IInventoryService inventoryService,
            IStoryContextBuilder storyContextBuilder,
            IPromptBuilder promptBuilder,
            ILogger<StoryService> logger)
        {
            _storyRepository = storyRepository;
            _characterRepository = characterRepository;
            _bedrockService = bedrockService;
            _characterService = characterService;
            _inventoryService = inventoryService;
            _storyContextBuilder = storyContextBuilder;
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
                return BuildResponse(existingSession, character);
            }

            var session = new StorySession
            {
                sessionId = Guid.NewGuid().ToString("N"),
                characterId = character.characterId,
                currentLocation = DefaultLocation,
                currentChapterId = string.IsNullOrWhiteSpace(request.storyFileId) ? DefaultChapterId : request.storyFileId,
                currentNodeId = DefaultChapterId,
                storyContext = string.Empty,
                status = "Active",
                updatedAt = DateTime.UtcNow,
                storyVersion = string.IsNullOrWhiteSpace(request.storyFileId) ? "1.0" : request.storyFileId,
                storySummary = "Mở đầu cuộc phiêu lưu tại tàn tích cổ.",
                sourceType = "AI"
            };

            session.storyContext = await GenerateNarrativeAsync(
                DefaultSystemPrompt,
                $"Start a new story for character {character.name} (level {character.level}) in {session.currentLocation}. Story file: {session.currentChapterId}.",
                "Bạn đứng trước cánh cổng cũ nát của Ancient Ruins. Một luồng khí lạnh toát thổi ra.");

            await _storyRepository.SaveSessionAsync(session);
            _logger.LogInformation("Story session started: {SessionId} for character: {CharacterId}", session.sessionId, character.characterId);

            return BuildResponse(session, character);
        }

        public async Task<StoryActionResponse> ProcessActionAsync(StoryActionRequest request)
        {
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
            var narrative = await GenerateNarrativeAsync(context);

            await SaveStoryTurnAsync(context, narrative);
            await UpdateSessionAsync(context, narrative);

            return BuildResponse(context);
        }

        private async Task<StoryActionResponse> ProcessChoiceActionAsync(
            StoryActionRequest request,
            Character character,
            StorySession session)
        {
            var (narrativeText, nextNodeId, goldDelta, hpDelta, expDelta, triggerBattle) = ResolveChoice(request.choiceIndex);

            character.gold = Math.Max(0, character.gold + goldDelta);
            character.hp = Math.Clamp(character.hp + hpDelta, 0, character.maxHp);
            await _characterService.ApplyExperienceAndLevelUp(character, expDelta);

            var aiNarrative = await GenerateNarrativeAsync(
                DefaultSystemPrompt,
                $"Character: {character.name} (Level {character.level}), Location: {session.currentLocation}, Current node: {session.currentNodeId}, Action: choice {request.choiceIndex}, Player input: {request.playerInput}",
                narrativeText);

            session.currentNodeId = nextNodeId;
            session.storyContext = aiNarrative;
            session.storySummary = UpdateStorySummary(session.storySummary, aiNarrative);
            session.updatedAt = DateTime.UtcNow;
            await _storyRepository.SaveSessionAsync(session);

            var action = new StoryAction
            {
                actionId = Guid.NewGuid().ToString("N"),
                sessionId = session.sessionId,
                playerInput = request.playerInput,
                aiResponse = aiNarrative,
                turnNumber = 0,
                actionType = "choice",
                metadataJson = "{}",
                createdAt = DateTime.UtcNow
            };
            await _storyRepository.SaveActionAsync(action);

            var response = BuildResponse(session, character);
            response.triggerBattle = triggerBattle;
            if (triggerBattle)
            {
                response.bossId = "boss_goblin_chief";
            }

            return response;
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

            var promptContext = await _storyContextBuilder.BuildAsync(
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

        private async Task<string> GenerateNarrativeAsync(StoryActionProcessingContext context)
        {
            var prompt = _promptBuilder.Build(context.PromptContext);
            return await GenerateNarrativeAsync(DefaultSystemPrompt, prompt, context.Session.storyContext ?? string.Empty);
        }

        private async Task SaveStoryTurnAsync(StoryActionProcessingContext context, string narrative)
        {
            var action = new StoryAction
            {
                actionId = Guid.NewGuid().ToString("N"),
                sessionId = context.Session.sessionId,
                playerInput = context.PlayerInput,
                aiResponse = narrative,
                turnNumber = (context.RecentActions?.Count ?? 0) + 1,
                actionType = "player_action",
                metadataJson = "{}",
                createdAt = DateTime.UtcNow
            };

            await _storyRepository.SaveActionAsync(action);
        }

        private async Task UpdateSessionAsync(StoryActionProcessingContext context, string narrative)
        {
            context.Session.storyContext = narrative;
            context.Session.storySummary = UpdateStorySummary(context.Session.storySummary, narrative);
            context.Session.updatedAt = DateTime.UtcNow;

            await _storyRepository.SaveSessionAsync(context.Session);
        }

        private static StoryActionResponse BuildResponse(StoryActionProcessingContext context)
        {
            return BuildResponse(context.Session, context.Character);
        }

        private static (string narrative, string nextNode, int gold, int hp, int exp, bool triggerBattle) ResolveChoice(int choiceIndex)
        {
            return choiceIndex switch
            {
                0 => ("Bạn giơ vũ khí lên và xông thẳng vào sương mù!", "battle_path", 0, 0, 25, true),
                1 => ("Bạn kiểm tra các ký tự kỳ lạ trên sàn đá, phát hiện ngăn bí mật chứa vàng cổ!", "investigate_path", 15, 0, 15, false),
                _ => ("Bạn đốt lửa nghỉ ngơi. Sức khỏe phục hồi nhưng tốn 5 vàng mua lương khô.", "rest_path", -5, 18, 5, false)
            };
        }

        private async Task<string> GenerateNarrativeAsync(string systemPrompt, string userPrompt, string fallbackNarrative)
        {
            try
            {
                if (await _bedrockService.IsAvailableAsync())
                {
                    var narrative = await _bedrockService.GenerateNarrativeAsync(systemPrompt, userPrompt);
                    if (!string.IsNullOrWhiteSpace(narrative))
                    {
                        return narrative;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bedrock AI fallback triggered for story prompt");
            }

            return fallbackNarrative;
        }

        private static string UpdateStorySummary(string? currentSummary, string narrativeText)
        {
            if (string.IsNullOrWhiteSpace(currentSummary))
            {
                return narrativeText;
            }

            var summary = currentSummary.Trim();
            var addition = narrativeText.Trim();
            if (summary.Length > 700)
            {
                summary = summary[..700];
            }

            return $"{summary} | {addition}";
        }

        private static StoryActionResponse BuildResponse(StorySession session, Character character)
        {
            return new StoryActionResponse
            {
                sessionId = session.sessionId,
                currentNodeId = session.currentNodeId,
                currentLocation = session.currentLocation,
                narrativeText = session.storyContext,
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

            public PromptContext PromptContext { get; init; }
        }
    }
}
