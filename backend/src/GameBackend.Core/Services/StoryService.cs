using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.AIStory.Builder;
using GameBackend.Core.AIStory;
using GameBackend.Core.AIStory.DTOs;
using GameBackend.Core.AIStory.Services;
using GameBackend.Core.Services.Parsing;
using GameShared.DTOs.Character;
using GameShared.DTOs.Story;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
    public class StoryService : IStoryService
    {
        private const string DefaultLocation = "ancient_cave";
        private const string DefaultChapterId = "chapter_1";

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
        private readonly IContentService _contentService;
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
            IContentService contentService,
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
            _contentService = contentService;
            _logger = logger;
        }

        private static readonly Dictionary<string, List<string>> LocationMobs = new()
        {
            // Chapter 1
            { "ancient_cave",      new() { "mob_cave_spider", "mob_goblin_scout" } },
            { "forgotten_temple",  new() { "mob_shadow_spirit", "mob_temple_golem", "mob_goblin_scout", "mob_goblin_guard", "mob_cave_spider" } },
            { "goblin_hideout",    new() { "mob_goblin_guard", "mob_goblin_scout" } },
            // Chapter 2
            { "sunken_shipwreck", new() { "mob_void_remnant", "mob_drowned_sailor", "mob_mutated_crab", "mob_shadow_spirit" } },
            { "abyssal_trench",   new() { "mob_void_remnant", "mob_abyssal_spirit", "mob_drowned_sailor" } },
            { "coral_palace",     new() { "mob_shadow_spirit", "mob_void_remnant" } },
            // Chapter 3
            { "sulfur_mines",     new() { "mob_young_dragon", "mob_fire_lizard", "mob_fire_raptor" } },
            { "obsidian_peaks",   new() { "mob_adult_dragon", "mob_young_dragon", "mob_fire_raptor" } },
            { "dragon_nest",      new() { "mob_adult_dragon", "mob_young_dragon" } },
        };

        private readonly Random _randomEncounterGenerator = new();

        /// <summary>
        /// Trả về bossId của chapter boss tương ứng với location hiện tại.
        /// </summary>
        private static string GetChapterBossId(string location)
        {
            return (location ?? "").ToLowerInvariant() switch
            {
                // Chapter 1
                "forgotten_temple" => "shadow_demon",
                "goblin_hideout"   => "goblin_king",
                // Chapter 2
                "coral_palace"     => "shadow_demon",
                // Chapter 3
                "dragon_nest"      => "dragon_king",
                _                  => "goblin_king"  // fallback
            };
        }

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
            Character? character = null;
            StorySession? session = null;
            try
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(request.characterId))
                    {
                        character = await _characterRepository.GetByIdAsync(request.characterId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch character {CharacterId} from DB, using fallback character", request.characterId);
                }

                if (character == null)
                {
                    character = new Character
                    {
                        characterId = string.IsNullOrWhiteSpace(request.characterId) ? "demo_char_id" : request.characterId,
                        userId = "demo_user",
                        name = "Adventurer",
                        level = 1,
                        hp = 100,
                        maxHp = 100,
                        attack = 15,
                        defense = 5,
                        gold = 50,
                        className = "Adventurer",
                        status = "Alive",
                        currentLocationId = DefaultLocation
                    };
                }

                try
                {
                    session = await _storyRepository.GetSessionByCharacterIdAsync(character.characterId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch session for character {CharacterId}", character.characterId);
                }

                // Nếu forceNewSession = true (người chơi chết + bấm Back to Menu):
                // Xóa session cũ và reset level nhân vật về 1
                if (request.forceNewSession && session != null)
                {
                    _logger.LogInformation("forceNewSession=true: Deleting old session {SessionId} for character {CharacterId}", session.sessionId, character.characterId);
                    try
                    {
                        await _storyRepository.DeleteSessionByCharacterIdAsync(character.characterId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete old session for character {CharacterId}", character.characterId);
                    }

                    // Reset level về 1 và stats cơ bản trên DB
                    character.level = 1;
                    character.experience = 0;
                    character.hp = 100;
                    character.maxHp = 100;
                    character.attack = 15;
                    character.defense = 5;
                    character.status = "Alive";
                    character.currentLocationId = DefaultLocation;
                    try
                    {
                        await _characterRepository.SaveAsync(character);
                        _logger.LogInformation("Reset character {CharacterId} to Level 1 after death.", character.characterId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not reset character stats for {CharacterId}", character.characterId);
                    }

                    session = null; // Bắt buộc tạo session mới bên dưới
                }

                if (session != null && session.status == "Active" && !string.IsNullOrWhiteSpace(session.currentNodeId))

                {
                    // Resume: gọi AI để tạo đoạn tiếp theo phù hợp (không lặp lại storySummary)
                    _logger.LogInformation("Resuming existing session {SessionId} at node {NodeId}", session.sessionId, session.currentNodeId);

                    var inventoryResponse = await _inventoryService.GetInventoryAsync(character.characterId);
                    var inventoryItems = (inventoryResponse?.slots ?? new List<GameShared.DTOs.Inventory.InventorySlot>())
                        .Select(slot => GameShared.Config.GameConstants.GetItemById(slot.itemId) ?? new Item { itemId = slot.itemId, name = slot.itemId, rarity = "Common", itemType = "General" })
                        .ToList();

                    var allRecentActions = await _storyRepository.GetActionsBySessionIdAsync(session.sessionId);
                    var recentActions = allRecentActions
                        .OrderByDescending(action => action.createdAt)
                        .Take(6)
                        .OrderBy(action => action.createdAt)
                        .ToList();

                    string resumeSystemEvent = BuildQuestItemDirective(inventoryItems, session.currentLocation ?? "ancient_cave");

                    var resumeContext = new StoryActionProcessingContext
                    {
                        Character = character,
                        Session = session,
                        PlayerInput = "[SYSTEM: Người chơi vừa quay trở lại game. Hãy tiếp tục câu chuyện từ vị trí hiện tại, nối tiếp diễn biến gần nhất mà không lặp lại bối cảnh cũ.]",
                        RecentActions = recentActions,
                        PromptContext = await _gamePromptContextBuilder.BuildAsync(character, inventoryItems, recentActions, session, "resume", resumeSystemEvent)
                    };

                    var resumeResponse = await GenerateStoryAiResponseAsync(resumeContext, "resume");
                    resumeResponse = await _gameRuleValidator.ValidateAndSanitizeAsync(session, character, resumeResponse);

                    try { await _storyStateUpdater.ApplyAsync(session, character, resumeResponse); }
                    catch (Exception ex) { _logger.LogWarning(ex, "Could not persist resume state"); }

                    await SaveStoryTurnAsync(resumeContext, resumeResponse, "resume");

                    return BuildResponse(resumeContext, resumeResponse.NarrativeText ?? session.storySummary ?? string.Empty, resumeResponse);
                }

                var startLocation = string.IsNullOrWhiteSpace(character.currentLocationId) || character.currentLocationId == "spawn_village" || character.currentLocationId == "ancient_cave"
                    ? "prologue"
                    : character.currentLocationId;

                var isPrologue = startLocation == "prologue";

                session = new StorySession
                {
                    sessionId = Guid.NewGuid().ToString("N"),
                    characterId = character.characterId,
                    currentLocation = startLocation,
                    currentChapterId = isPrologue ? "prologue" : (string.IsNullOrWhiteSpace(request.storyFileId) ? DefaultChapterId : request.storyFileId),
                    currentNodeId = isPrologue ? "prologue" : DefaultChapterId,
                    status = "Active",
                    updatedAt = DateTime.UtcNow,
                    storyVersion = string.IsNullOrWhiteSpace(request.storyFileId) ? "1.0" : request.storyFileId,
                    storySummary = isPrologue ? "Mở đầu cuộc phiêu lưu tại Sảnh Khởi Nguyên." : "Bắt đầu cuộc phiêu lưu.",
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

                try
                {
                    await _storyStateUpdater.ApplyAsync(session, character, openingResponse);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not persist story state to DB, continuing in-memory");
                }

                await SaveStoryTurnAsync(openingContext, openingResponse, "opening");

                _logger.LogInformation("Story session started: {SessionId} for character: {CharacterId}", session.sessionId, character.characterId);

                return BuildResponse(openingContext, openingResponse.NarrativeText, openingResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "StartStoryAsync error for request character {CharacterId}. Returning safe fallback response.", request.characterId);
                var safeSession = session ?? new StorySession
                {
                    sessionId = Guid.NewGuid().ToString("N"),
                    characterId = request.characterId ?? "demo_char_id",
                    currentLocation = DefaultLocation,
                    currentChapterId = DefaultChapterId,
                    currentNodeId = DefaultChapterId,
                    status = "Active",
                    updatedAt = DateTime.UtcNow,
                    storySummary = "Mở đầu cuộc phiêu lưu tại tàn tích cổ."
                };
                var safeChar = character ?? new Character
                {
                    characterId = request.characterId ?? "demo_char_id",
                    name = "Adventurer",
                    level = 1,
                    hp = 100,
                    maxHp = 100,
                    gold = 999999
                };
                return BuildResponse(safeSession, safeChar, "Bạn bước vào khu vực đầu tiên của Etherea, Rừng Thì Thầm. Ánh trăng chiếu xuống, tạo nên bóng đổ rập khuôn giữa những cây cổ thụ.");
            }
        }

        public async Task<StoryActionResponse> ProcessActionAsync(StoryActionRequest request)
        {
            try
            {
                await _characterService.EnsureAliveOrAutoReviveAsync(request.characterId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EnsureAliveOrAutoReviveAsync warning for character: {CharacterId}", request.characterId);
            }

            Character? character = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(request.characterId))
                {
                    character = await _characterRepository.GetByIdAsync(request.characterId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch character {CharacterId}", request.characterId);
            }

            if (character == null)
            {
                character = new Character
                {
                    characterId = string.IsNullOrWhiteSpace(request.characterId) ? "demo_char_id" : request.characterId,
                    userId = "demo_user",
                    name = "Adventurer",
                    level = 1,
                    hp = 100,
                    maxHp = 100,
                    attack = 15,
                    defense = 5,
                    gold = 50,
                    className = "Adventurer",
                    status = "Alive",
                    currentLocationId = DefaultLocation
                };
            }

            StorySession? session = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(request.sessionId))
                {
                    session = await _storyRepository.GetSessionByIdAsync(request.sessionId);
                }

                if (session == null && !string.IsNullOrWhiteSpace(request.characterId))
                {
                    session = await _storyRepository.GetSessionByCharacterIdAsync(request.characterId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not fetch session for session {SessionId} / character {CharacterId}", request.sessionId, request.characterId);
            }

            if (session == null)
            {
                session = new StorySession
                {
                    sessionId = string.IsNullOrWhiteSpace(request.sessionId) ? Guid.NewGuid().ToString("N") : request.sessionId,
                    characterId = character.characterId,
                    currentLocation = DefaultLocation,
                    currentChapterId = DefaultChapterId,
                    currentNodeId = DefaultChapterId,
                    status = "Active",
                    updatedAt = DateTime.UtcNow,
                    storyVersion = "1.0",
                    storySummary = "Mở đầu cuộc phiêu lưu tại tàn tích cổ.",
                    sourceType = "AI"
                };
            }

            // Trừ 5 Gold mỗi lượt AI kể chuyện (StoryCostPerTurn)
            if (character.gold >= GameShared.Config.GameConstants.StoryCostPerTurn)
            {
                character.gold -= GameShared.Config.GameConstants.StoryCostPerTurn;
            }
            else
            {
                character.gold = 0; // Không đủ vàng vẫn cho chơi nhưng trừ hết vàng còn lại
            }
            try
            {
                await _characterRepository.SaveAsync(character);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not persist gold deduction for character {CharacterId}", character.characterId);
            }

            var inventoryResponse = await _inventoryService.GetInventoryAsync(character.characterId);
            var inventorySlots = inventoryResponse?.slots ?? new List<GameShared.DTOs.Inventory.InventorySlot>();
            var inventoryItems = inventorySlots
                .Select(slot => GameShared.Config.GameConstants.GetItemById(slot.itemId) ?? new Item { itemId = slot.itemId, name = slot.itemId, rarity = "Common", itemType = "General" })
                .ToList();

            string? systemInjectedEvent = null;
            bool isInBossRoom = (session.currentNodeId ?? "").Equals("boss_room", StringComparison.OrdinalIgnoreCase);

            var recentActionsList = await _storyRepository.GetActionsBySessionIdAsync(session.sessionId);
            var last2Actions = recentActionsList?.OrderByDescending(a => a.createdAt).Take(2).ToList();
            bool recentlyFoughtBattle = last2Actions != null && last2Actions.Any(a => string.Equals(a.actionType, "battle_result", StringComparison.OrdinalIgnoreCase));

            // 1. Dịch choiceIndex ra playerInput nếu playerInput bị trống
            if (string.IsNullOrWhiteSpace(request.playerInput))
            {
                var latestAction = recentActionsList?.OrderByDescending(a => a.createdAt).FirstOrDefault();
                if (latestAction != null)
                {
                    try
                    {
                        var rawJson = !string.IsNullOrWhiteSpace(latestAction.metadataJson) ? latestAction.metadataJson : latestAction.aiResponse;
                        var parsed = GameBackend.Core.Services.Parsing.StoryAiResponseParser.Parse(rawJson, session, "choice");
                        if (parsed.Choices != null && request.choiceIndex >= 0 && request.choiceIndex < parsed.Choices.Count)
                        {
                            var selectedChoice = parsed.Choices[request.choiceIndex];
                            request.playerInput = $"{selectedChoice.label}: {selectedChoice.description}";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse choices from previous action");
                    }
                }

                if (string.IsNullOrWhiteSpace(request.playerInput))
                {
                    request.playerInput = request.choiceIndex switch
                    {
                        0 => "Tấn công quái vật",
                        1 => "Điều tra xung quanh",
                        _ => "Nghỉ ngơi hồi phục"
                    };
                }
            }

            // 2. Kiểm tra ý định chiến đấu
            bool intentToFight = false;
            if (!string.IsNullOrWhiteSpace(request.playerInput))
            {
                var lowerInput = request.playerInput.ToLowerInvariant();
                if (lowerInput.Contains("chiến đấu") || lowerInput.Contains("tấn công") || lowerInput.Contains("đánh") || lowerInput.Contains("tiêu diệt"))
                {
                    intentToFight = true;
                }
            }

            // 3. Xử lý các sự kiện (Boss Room, Ambush, v.v.)
            if (isInBossRoom && !recentlyFoughtBattle)
            {
                var chapterBossId = GetChapterBossId(session.currentLocation);
                var chapterBoss = GameShared.Config.GameConstants.BossCatalog.FirstOrDefault(b => b.bossId == chapterBossId);
                if (chapterBoss != null)
                {
                    int bossLvl = GameShared.Config.GameConstants.CalculateBossLevel(character.level, chapterBoss.rarity, chapterBossId);
                    if (intentToFight)
                    {
                        systemInjectedEvent = $"[LỆNH HỆ THỐNG] Người chơi đã chọn TẤN CÔNG BOSS! " +
                            $"BẮT BUỘC đặt triggerBattle: true, bossId: '{chapterBossId}', bossName: '{chapterBoss.name}', bossLevel: {bossLvl}. " +
                            $"Chỉ miêu tả cảnh người chơi lao vào chuẩn bị chiến đấu, tuyệt đối không miêu tả diễn biến trận đánh!";
                    }
                    else
                    {
                        systemInjectedEvent = $"[SỰ KIỆN BOSS PHÒNG CUỐI] Người chơi đã bước vào phòng Boss và đối mặt với {chapterBoss.name} (Cấp {bossLvl})! " +
                            $"Hãy miêu tả sự xuất hiện oai vệ, đáng sợ của Boss này chặn đường đi. " +
                            $"BẮT BUỘC đặt triggerBattle: false, và tạo ra các lựa chọn (choices) để người chơi quyết định (ví dụ: Tấn công, Chuẩn bị thủ thế, v.v.).";
                    }
                }
            }
            else
            {
                systemInjectedEvent = BuildQuestItemDirective(inventoryItems, session.currentLocation ?? "ancient_cave");

                if (recentlyFoughtBattle)
                {
                    systemInjectedEvent += " [HẬU CHIẾN] Người chơi vừa hoàn thành một trận chiến ác liệt. Hãy miêu tả ngắn gọn cảnh họ thở phào hoặc thu thập chiến lợi phẩm trước khi đưa ra các lựa chọn để tiếp tục hành trình.";
                }
                else if (session.status == "Active")
                {
                    if (intentToFight)
                    {
                        systemInjectedEvent += " [LỆNH HỆ THỐNG] Người chơi đã CHỦ ĐỘNG chọn hành động chiến đấu. Bạn BẮT BUỘC phải thiết lập triggerBattle: true và điền bossId tương ứng với kẻ địch mà họ đang đối đầu (nếu có).";
                    }
                    else
                    {
                        // Cơ chế Ambush (15% tỷ lệ phục kích khi người chơi chọn không chiến đấu)
                        var rnd = new Random();
                        if (rnd.Next(100) < 15) // 15% chance
                        {
                            var mobId = RollRandomEncounter(session.currentLocation);
                            if (mobId != null)
                            {
                                var mob = GameShared.Config.GameConstants.BossCatalog.FirstOrDefault(b => b.bossId == mobId);
                                if (mob != null)
                                {
                                    systemInjectedEvent += $" [SỰ KIỆN PHỤC KÍCH] Mặc dù người chơi không muốn đánh, nhưng một con {mob.name} quá nhanh và hung hãn đã lao ra chặn đường! " +
                                        $"BẮT BUỘC miêu tả lý do tại sao người chơi không thể né tránh. " +
                                        $"ĐỒNG THỜI thiết lập triggerBattle: false, bossId: '{mobId}', và cung cấp các lựa chọn cho người chơi (ví dụ: Chiến đấu, Cố gắng bỏ chạy).";
                                }
                            }
                        }
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
            StoryAiResponse aiResponse;
            try
            {
                aiResponse = await GenerateStoryAiResponseAsync(context, "player_action");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating AI response in ProcessFreeFormActionAsync, falling back");
                aiResponse = CreateFallbackResponse(context, "Bạn tiếp tục cuộc phiêu lưu trong bóng tối...");
            }

            if (string.IsNullOrWhiteSpace(aiResponse.NarrativeText))
            {
                aiResponse = CreateFallbackResponse(context, "Bạn tiếp tục cuộc phiêu lưu trong bóng tối...");
            }

            try
            {
                aiResponse = await _gameRuleValidator.ValidateAndSanitizeAsync(session, character, aiResponse);
                await _storyStateUpdater.ApplyAsync(session, character, aiResponse);
                await SaveStoryTurnAsync(context, aiResponse);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating/applying story state update, continuing with generated narrative");
            }

            return BuildResponse(context, aiResponse.NarrativeText, aiResponse);
        }

        private async Task<StoryActionResponse> ProcessChoiceActionAsync(
            StoryActionRequest request,
            Character character,
            StorySession session,
            string? systemInjectedEvent)
        {
            var context = await LoadGameContextAsync(request, character, session, systemInjectedEvent);
            StoryAiResponse aiResponse;
            try
            {
                aiResponse = await GenerateStoryAiResponseAsync(context, "choice");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating AI response in ProcessChoiceActionAsync, falling back");
                aiResponse = CreateFallbackResponseFromChoice(request.choiceIndex, context);
            }

            if (string.IsNullOrWhiteSpace(aiResponse.NarrativeText))
            {
                aiResponse = CreateFallbackResponseFromChoice(request.choiceIndex, context);
            }

            try
            {
                aiResponse = await _gameRuleValidator.ValidateAndSanitizeAsync(session, character, aiResponse);
                await _storyStateUpdater.ApplyAsync(session, character, aiResponse);
                await SaveStoryTurnAsync(context, aiResponse, "choice");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating/applying story state update, continuing with generated narrative");
            }

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
                .Select(slot =>
                {
                    var catalogItem = GameShared.Config.GameConstants.GetItemById(slot.itemId);
                    return catalogItem ?? new Item
                    {
                        itemId = slot.itemId,
                        name = slot.itemId,
                        rarity = "Common",
                        itemType = "General"
                    };
                })
                .ToList();

            var allRecentActions = await _storyRepository.GetActionsBySessionIdAsync(session.sessionId);
            var recentActions = allRecentActions
                .OrderByDescending(action => action.createdAt)
                .Take(6)
                .OrderBy(action => action.createdAt)
                .ToList();

            // GENERIC CHOICE PROGRESSION: Tự động trích xuất nextNodeId từ lựa chọn của lượt trước
            if (request.choiceIndex >= 0 && recentActions.Count > 0)
            {
                var lastAction = recentActions.LastOrDefault();
                if (lastAction != null && !string.IsNullOrWhiteSpace(lastAction.metadataJson))
                {
                    try
                    {
                        var lastAiResponse = StoryAiResponseParser.Parse(lastAction.metadataJson, session, "choice");
                        if (lastAiResponse?.Choices != null && request.choiceIndex < lastAiResponse.Choices.Count)
                        {
                            var chosenOption = lastAiResponse.Choices[request.choiceIndex];
                            if (!string.IsNullOrWhiteSpace(chosenOption?.nextNodeId))
                            {
                                var targetNode = chosenOption.nextNodeId.Trim();
                                if (await _contentService.LocationExistsAsync(targetNode))
                                {
                                    session.currentLocation = targetNode;
                                    session.currentNodeId = targetNode;
                                    character.currentLocationId = targetNode;
                                    _logger.LogInformation("Generic Choice Progression: Moved player location to '{Location}' based on choice index {Index}", targetNode, request.choiceIndex);
                                }
                                else
                                {
                                    session.currentNodeId = targetNode;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not parse choices from last action metadata for choice progression");
                    }
                }
            }

            // Tự động kiểm tra nếu playerInput chứa locationId hợp lệ
            if (!string.IsNullOrWhiteSpace(request.playerInput))
            {
                var cleanInput = request.playerInput.Trim().ToLowerInvariant().Replace(" ", "_");
                if (await _contentService.LocationExistsAsync(cleanInput))
                {
                    session.currentLocation = cleanInput;
                    session.currentNodeId = cleanInput;
                    character.currentLocationId = cleanInput;
                }
            }

            // (Removed buggy Key Item auto-progression that conflicted with AI story constraints)

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
            var (systemPrompt, userPrompt) = _promptBuilder.Build(context.PromptContext);

            userPrompt += "\n\nBẮT BUỘC trả về một khối markdown JSON (```json ... ```) chứa DUY NHẤT một đối tượng JSON hợp lệ theo schema sau. BẮT BUỘC TRẢ VỀ 3 LỰA CHỌN TRONG MẢNG choices (nếu triggerBattle=false):\n" +
                      "{\n" +
                      "  \"narrativeText\": \"Vietnamese story response description text here\",\n" +
                      "  \"currentNodeId\": \"current node ID\",\n" +
                      "  \"currentLocation\": \"current location ID\",\n" +
                      "  \"currentChapterId\": \"current chapter ID\",\n" +
                      "  \"storySummary\": \"brief updated summary of the story so far\",\n" +
                      "  \"actionType\": \"player_action\",\n" +
                      "  \"triggerBattle\": false (CHỈ ĐẶT LÀ true NẾU NGƯỜI CHƠI VỪA CHỌN TẤN CÔNG),\n" +
                      "  \"bossId\": \"boss_id_if_battle_triggered_else_null\",\n" +
                      "  \"bossName\": \"boss_name_if_battle_triggered_else_null\",\n" +
                      "  \"bossLevel\": null_or_integer,\n" +
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

            context.BuiltPrompt = $"[SYSTEM PROMPT]\n{systemPrompt}\n\n[USER PROMPT]\n{userPrompt}";

            _logger.LogInformation("==================== [FULL AI PROMPT] ====================\nSYSTEM:\n{SystemPrompt}\n\nUSER:\n{UserPrompt}\n============================================================", systemPrompt, userPrompt);

            var rawResponse = await GenerateRawAiResponseAsync(systemPrompt, userPrompt, context.Session.storySummary ?? string.Empty);
            
            context.BuiltPrompt += $"\n\n[RAW AI RESPONSE]\n{rawResponse}";

            _logger.LogInformation("==================== [RAW AI RESPONSE] ====================\n{RawResponse}\n============================================================", rawResponse);
            return StoryAiResponseParser.Parse(rawResponse, context.Session, defaultActionType, _logger);
        }

        private async Task SaveStoryTurnAsync(StoryActionProcessingContext context, StoryAiResponse aiResponse, string defaultActionType = "player_action")
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to persist story turn to DB, continuing without throwing");
            }
        }

        private static StoryActionResponse BuildResponse(StoryActionProcessingContext context, string narrativeText, StoryAiResponse aiResponse)
        {
            var response = BuildResponse(context.Session, context.Character, narrativeText, aiResponse.Choices);
            response.triggerBattle = aiResponse.TriggerBattle;
            response.bossId = aiResponse.BossId;
            response.bossName = aiResponse.BossName;
            response.bossLevel = aiResponse.BossLevel;
            response.debugPrompt = context.BuiltPrompt;
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
                    : BuildFallbackChoices(session.currentLocation),
                triggerBattle = false
            };
        }

        private static List<StoryChoiceOption> BuildFallbackChoices(string? location)
        {
            var loc = (location ?? "").ToLowerInvariant();
            if (loc.Contains("sunken_shipwreck") || loc.Contains("shipwreck"))
            {
                return new List<StoryChoiceOption>
                {
                    new() { label = "Khám phá tầng dưới xác tàu", description = "Tiến sâu vào bên trong boong tàu", nextNodeId = "shipwreck_deck" },
                    new() { label = "Lặn xuống Rãnh Sâu Vô Tận", description = "Rời khỏi tàu đắm và tiến vào Rãnh Sâu", nextNodeId = "abyssal_trench" },
                    new() { label = "Kiểm tra xung quanh", description = "Tìm kiếm các món đồ hữu ích", nextNodeId = "investigate_path" }
                };
            }
            if (loc.Contains("abyssal_trench"))
            {
                return new List<StoryChoiceOption>
                {
                    new() { label = "Tiến vào Cung Điện San Hô", description = "Đi về phía ánh sáng tím huyền bí", nextNodeId = "coral_palace" },
                    new() { label = "Quay lại Xác Tàu Đắm", description = "Trở về khu vực an toàn hơn", nextNodeId = "sunken_shipwreck" },
                    new() { label = "Cẩn trọng quan sát", description = "Dò tìm sinh vật biển sâu", nextNodeId = "investigate_path" }
                };
            }
            return new List<StoryChoiceOption>
            {
                new() { label = "Tiến lên phía trước", description = "Tiếp tục khám phá khu vực", nextNodeId = "explore_path" },
                new() { label = "Kiểm tra xung quanh", description = "Tìm kiếm dấu vết bí ẩn", nextNodeId = "investigate_path" },
                new() { label = "Nghỉ ngơi quan sát", description = "Tạm dừng để lấy lại sức mạnh", nextNodeId = "rest_path" }
            };
        }


        private string BuildQuestItemDirective(IEnumerable<Item>? inventoryItems, string currentLocation)
        {
            if (currentLocation.Equals("goblin_hideout", StringComparison.OrdinalIgnoreCase))
            {
                 return $"[TIẾN VÀO SÀO HUYỆT GOBLIN] Người chơi hiện đang ở sào huyệt của kẻ địch. " +
                        $"BẮT BUỘC phải cung cấp 1 Lựa chọn (Choice) có nextNodeId='boss_room' để tiến vào Phòng Ngai Vàng khi người chơi đã sẵn sàng. " +
                        $"Các lựa chọn khác có thể là thám hiểm hoặc đối phó với quái vật xung quanh.";
            }

            var questItems = inventoryItems?
                .Where(i => i != null && string.Equals(i.itemType, "Quest", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<Item>();

            if (questItems.Count > 0)
            {
                var questNames = string.Join(", ", questItems.Select(i => $"'{i.name}' ({i.itemId})"));
                return $"[NHẬN DIỆN VẬT PHẨM NHIỆM VỤ THỰC TẾ TRONG TÚI ĐỒ] Người chơi ĐÃ SỞ HỮU các Vật Phẩm Nhiệm Vụ: {questNames}. " +
                       $"Bạn BẮT BUỘC phải đối chiếu với Quy tắc Tiến trình Chương trong system_prompt để kiểm tra xem các vật phẩm này có mở khóa địa điểm/lối đi tiếp theo từ vị trí hiện tại ({currentLocation}) hay không. " +
                       $"NẾU vật phẩm nhiệm vụ cho phép di chuyển sang địa điểm mới, bạn BẮT BUỘC phải mô tả lối đi mở ra và cung cấp Lựa chọn (Choice) tương ứng trong danh sách choices để người chơi tiến sang địa điểm tiếp theo.";
            }

            string targetMonster = "một con quái vật mạnh mẽ";
            string targetItem = "Vật Phẩm Nhiệm Vụ";
            string nextLocation = "địa điểm tiếp theo";
            
            if (currentLocation.Equals("ancient_cave", StringComparison.OrdinalIgnoreCase))
            {
                targetMonster = "Nhện Hang Động (Cave Spider)";
                targetItem = "Chìa Khóa Cổ Xưa (item_ancient_key)";
                nextLocation = "forgotten_temple";
            }
            else if (currentLocation.Equals("forgotten_temple", StringComparison.OrdinalIgnoreCase))
            {
                targetMonster = "Golem Đền Thờ (Temple Golem)";
                targetItem = "Lõi Nguyên Tố (item_elemental_core)";
                nextLocation = "goblin_hideout";
            }
            else if (currentLocation.Equals("sunken_shipwreck", StringComparison.OrdinalIgnoreCase))
            {
                targetMonster = "Thủy Thủ Chết Đuối hoặc Cua Đột Biến";
                targetItem = "Hải Đồ Biển Sâu (item_sea_compass)";
                nextLocation = "abyssal_trench";
            }
            else if (currentLocation.Equals("abyssal_trench", StringComparison.OrdinalIgnoreCase))
            {
                targetMonster = "Tàn Dư Hư Không (Void Remnant)";
                targetItem = "Pha Lê Hư Không (item_void_crystal)";
                nextLocation = "coral_palace";
            }
            else if (currentLocation.Equals("coral_palace", StringComparison.OrdinalIgnoreCase))
            {
                targetMonster = "Oan Hồn Bóng Tối (Shadow Spirit)";
                targetItem = "Pha Lê Hư Không (dùng mở cổng ngai vàng)";
                nextLocation = "boss_room";
            }

            return $"[NHIỆM VỤ CỐT TRUYỆN: TÌM {targetItem.ToUpperInvariant()}] Người chơi CHƯA SỞ HỮU {targetItem}. " +
                   $"Bạn TUYỆT ĐỐI KHÔNG ĐƯỢC tạo lựa chọn cho phép sang khu vực mới ({nextLocation}) khi chưa thu thập {targetItem}! " +
                   $"THAY VÀO ĐÓ, BẠN BẮT BUỘC PHẢI DẪN TRUYỆN BẰNG CÁCH: Tiết lộ hoặc ám chỉ rằng {targetMonster} đang cất giữ {targetItem}. " +
                   $"Sau đó, cung cấp các Lựa chọn (choices) xoay quanh việc thám hiểm hoặc khiêu chiến {targetMonster} để giành lấy {targetItem}. " +
                   $"KHI TIÊU DIỆT HOẶC TÌM THẤY {targetItem}, HÃY TẠO ITEM NÀY TRONG INVENTORYCHANGES: [{{ \"itemId\": \"item_id_tương_ứng\", \"quantityDelta\": 1 }}].";

        }

        private sealed class StoryActionProcessingContext
        {
            public required Character Character { get; init; }

            public required StorySession Session { get; init; }

            public required string PlayerInput { get; init; }

            public List<StoryAction> RecentActions { get; init; } = new();

            public required GamePromptContext PromptContext { get; init; }

            public string? BuiltPrompt { get; set; }
        }
    }
}
