using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameShared.DTOs.Character;
using GameShared.DTOs.Story;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
    public class StoryService : IStoryService
    {
        private readonly IStoryRepository _storyRepository;
        private readonly ICharacterRepository _characterRepository;
        private readonly IBedrockService _bedrockService;
        private readonly ICharacterService _characterService;
        private readonly ILogger<StoryService> _logger;

        public StoryService(
            IStoryRepository storyRepository,
            ICharacterRepository characterRepository,
            IBedrockService bedrockService,
            ICharacterService characterService,
            ILogger<StoryService> logger)
        {
            _storyRepository = storyRepository;
            _characterRepository = characterRepository;
            _bedrockService = bedrockService;
            _characterService = characterService;
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
                currentLocation = "Ancient Ruins",
                currentNodeId = "intro",
                storyContext = "Bạn đứng trước cánh cổng cũ nát của Ancient Ruins. Một luồng khí lạnh toát thổi ra.",
                status = "Active",
                updatedAt = DateTime.UtcNow,
                storyVersion = "1.0",
                sourceType = "AI"
            };

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

            // Áp dụng hiệu ứng lựa chọn (tách từ StoryActionFunction logic)
            var (narrativeText, nextNodeId, goldDelta, hpDelta, expDelta, triggerBattle) = ResolveChoice(request.choiceIndex);

            character.gold = Math.Max(0, character.gold + goldDelta);
            character.hp = Math.Clamp(character.hp + hpDelta, 0, character.maxHp);
            await _characterService.ApplyExperienceAndLevelUp(character, expDelta);

            // Thử gọi AI nếu Bedrock khả dụng
            string aiNarrative = narrativeText;
            try
            {
                if (await _bedrockService.IsAvailableAsync())
                {
                    string prompt = $"Character: {character.name} (Level {character.level}), Location: {session.currentLocation}, Action: choice {request.choiceIndex}";
                    aiNarrative = await _bedrockService.GenerateNarrativeAsync(
                        "You are a dungeon master for a dark fantasy RPG game. Respond in Vietnamese.",
                        prompt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bedrock AI fallback triggered for session {SessionId}", session.sessionId);
                // Fallback: dùng narrativeText tĩnh đã tính ở trên
            }

            // Cập nhật session
            session.currentNodeId = nextNodeId;
            session.storyContext = aiNarrative;
            session.updatedAt = DateTime.UtcNow;
            await _storyRepository.SaveSessionAsync(session);

            // Lưu action log
            var action = new StoryAction
            {
                actionId = Guid.NewGuid().ToString("N"),
                sessionId = session.sessionId,
                playerInput = request.playerInput,
                aiResponse = aiNarrative,
                choiceIndex = request.choiceIndex,
                actionType = "choice",
                metadataJson = "{}",
                createdAt = DateTime.UtcNow
            };
            await _storyRepository.SaveActionAsync(action);

            var response = BuildResponse(session, character);
            response.triggerBattle = triggerBattle;
            return response;
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
    }
}
