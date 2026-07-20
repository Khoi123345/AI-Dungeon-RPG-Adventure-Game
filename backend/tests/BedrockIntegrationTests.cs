using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.BedrockRuntime;
using GameBackend.Core.AIStory.Builder.Impl;
using GameBackend.Core.Config;
using GameBackend.Core.Services;
using GameBackend.Core.Services.Parsing;
using GameBackend.Core.Services.Validation;
using GameShared.DTOs.Character;
using GameShared.DTOs.Story;
using GameShared.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;

namespace GameBackend.Tests
{
    public class BedrockIntegrationTests
    {
        private readonly ITestOutputHelper _output;

        public BedrockIntegrationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Test1_RealBedrockCall_And_WriteTxtOutput()
        {
            AmazonBedrockRuntimeClient? client = null;
            try
            {
                client = new AmazonBedrockRuntimeClient(RegionEndpoint.APSoutheast1);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Client init info: {ex.Message}");
            }

            var options = new BedrockOptions
            {
                Region = "ap-southeast-1",
                ModelId = "anthropic.claude-sonnet-4-5-20250929-v1:0",
                Temperature = 0.7f,
                MaxTokens = 1000,
                TopP = 0.9f
            };

            var bedrockService = new BedrockService(
                client!,
                Options.Create(options),
                NullLogger<BedrockService>.Instance);

            var systemPrompt = "You are a dungeon master for a dark fantasy RPG game. Return ONLY valid JSON for StoryAiResponse. No markdown, no code fences, no extra commentary. Use camelCase property names.";
            var userPrompt = "Người chơi quyết định tiến vào hang động tăm tối. Hãy tạo phản hồi diễn biến cốt truyện bằng JSON.";

            var rawJson = await bedrockService.GenerateNarrativeAsync(systemPrompt, userPrompt);
            _output.WriteLine($"Raw AI Response:\n{rawJson}");

            var outputDirectory = GetTestOutputDirectory();
            var filePath = Path.Combine(outputDirectory, "bedrock_real_call_result.txt");

            var logContent = $"=== BEDROCK CALL RESULT ({DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC) ===\n" +
                             $"ModelId: {options.ModelId}\n" +
                             $"Region: {options.Region}\n" +
                             $"RAW RESPONSE:\n{rawJson}\n";

            await File.WriteAllTextAsync(filePath, logContent);
            _output.WriteLine($"Result saved to file: {filePath}");

            Assert.False(string.IsNullOrWhiteSpace(rawJson), "Raw JSON response should not be empty");
        }

        [Fact]
        public async Task Test2_ClaudeResponse_To_Parser_Integration()
        {
            var options = new BedrockOptions
            {
                Region = "ap-southeast-1",
                ModelId = "anthropic.claude-sonnet-4-5-20250929-v1:0",
                Temperature = 0.7f,
                MaxTokens = 1000,
                TopP = 0.9f
            };

            AmazonBedrockRuntimeClient? client = null;
            try { client = new AmazonBedrockRuntimeClient(RegionEndpoint.APSoutheast1); } catch { }

            var bedrockService = new BedrockService(client!, Options.Create(options), NullLogger<BedrockService>.Instance);

            var systemPrompt = "You are a dungeon master for a dark fantasy RPG game. Return ONLY valid JSON for StoryAiResponse. No markdown, no code fences, no extra commentary. Use camelCase property names.";
            var userPrompt = "Người chơi kiểm tra hòm báu cổ trong hầm ngục. Tạo phản hồi JSON hợp lệ.";

            var rawJson = await bedrockService.GenerateNarrativeAsync(systemPrompt, userPrompt);

            var session = new StorySession
            {
                sessionId = "test_session_1",
                characterId = "char_1",
                currentLocation = "Dungeon Core",
                currentNodeId = "node_1",
                currentChapterId = "chapter_1",
                storySummary = "Mở đầu cuộc phiêu lưu"
            };

            var parsedResponse = StoryAiResponseParser.Parse(rawJson, session, "player_action", NullLogger<StoryService>.Instance);

            _output.WriteLine($"Parsed NarrativeText: {parsedResponse.NarrativeText}");
            _output.WriteLine($"Parsed ActionType: {parsedResponse.ActionType}");

            Assert.NotNull(parsedResponse);
            Assert.False(string.IsNullOrWhiteSpace(parsedResponse.NarrativeText), "Parsed NarrativeText must not be empty");
        }

        [Fact]
        public async Task Test3_FullPipeline_PromptBuilder_To_Bedrock_To_Parser_To_Validator_To_StateUpdater()
        {
            var character = new Character
            {
                characterId = "hero_1",
                userId = "user_1",
                name = "Kratos",
                level = 1,
                hp = 100,
                maxHp = 100,
                mp = 50,
                maxMp = 50,
                gold = 10,
                currentLocationId = "Ancient Ruins"
            };

            var session = new StorySession
            {
                sessionId = "session_full_pipeline",
                characterId = character.characterId,
                currentLocation = "Ancient Ruins",
                currentNodeId = "intro",
                currentChapterId = "chapter_1",
                status = "Active",
                storySummary = "Anh hùng bắt đầu cuộc hành trình tại tàn tích cổ."
            };

            // 1. PromptBuilder
            var promptBuilder = CreatePromptBuilder();
            var promptContext = new GamePromptContext
            {
                World = "Dark Fantasy World",
                CharacterInfo = $"Name: {character.name}, Level: {character.level}, HP: {character.hp}/{character.maxHp}",
                InventoryInfo = "Health Potion x1",
                Chapter = "Intro",
                Location = session.currentLocation,
                StorySummary = session.storySummary,
                RecentTurns = "Turn 1: Người chơi di chuyển vào sảnh chính.",
                UserAction = "Tấn công quái vật canh giữ báu vật"
            };

            var userPrompt = promptBuilder.Build(promptContext);
            var systemPrompt = "You are a dungeon master for a dark fantasy RPG game. Return ONLY valid JSON for StoryAiResponse. No markdown, no code fences, no extra commentary. Use camelCase property names.";

            // 2. Bedrock (Claude)
            AmazonBedrockRuntimeClient? client = null;
            try { client = new AmazonBedrockRuntimeClient(RegionEndpoint.APSoutheast1); } catch { }

            var options = new BedrockOptions { ModelId = "anthropic.claude-sonnet-4-5-20250929-v1:0", Temperature = 0.7f };
            var bedrockService = new BedrockService(client!, Options.Create(options), NullLogger<BedrockService>.Instance);
            var rawJson = await bedrockService.GenerateNarrativeAsync(systemPrompt, userPrompt);

            // 3. Parser
            var aiResponse = StoryAiResponseParser.Parse(rawJson, session, "player_action", NullLogger<StoryService>.Instance);
            Assert.NotNull(aiResponse);

            // 4. Validator
            var validator = new GameRuleValidator(
                new IGameRuleSubValidator[]
                {
                    new CharacterValidator(),
                    new StoryValidator()
                },
                NullLogger<GameRuleValidator>.Instance);

            var validatedResponse = await validator.ValidateAndSanitizeAsync(session, character, aiResponse);
            Assert.NotNull(validatedResponse);

            // 5. StateUpdater
            var fakeStoryRepo = new FakeStoryRepository();
            var fakeCharService = new FakeCharacterService();
            var fakeInvRepo = new FakeInventoryRepository();
            var fakeBattleRepo = new FakeBattleRepository();
            var fakeBossRepo = new FakeBossRepository();

            var stateUpdater = new StoryStateUpdater(
                fakeStoryRepo,
                fakeCharService,
                fakeInvRepo,
                fakeBattleRepo,
                fakeBossRepo,
                NullLogger<StoryStateUpdater>.Instance);

            await stateUpdater.ApplyAsync(session, character, validatedResponse);

            // Ghi log kết quả pipeline chi tiết theo chuẩn yêu cầu
            var jsonOptions = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var promptSection = $"=========== PROMPT ===========\n{userPrompt}\n";
            var rawSection = $"=========== RAW AI ===========\n{rawJson}\n";
            var parsedSection = $"=========== PARSED ===========\n{System.Text.Json.JsonSerializer.Serialize(aiResponse, jsonOptions)}\n";
            var validatedSection = $"=========== VALIDATED ===========\n{System.Text.Json.JsonSerializer.Serialize(validatedResponse, jsonOptions)}\n";
            var sessionSection = $"=========== FINAL SESSION ===========\n{System.Text.Json.JsonSerializer.Serialize(session, jsonOptions)}\n";
            var characterSection = $"=========== CHARACTER ===========\n{System.Text.Json.JsonSerializer.Serialize(character, jsonOptions)}\n";

            var fullLog = $"{promptSection}\n{rawSection}\n{parsedSection}\n{validatedSection}\n{sessionSection}\n{characterSection}";

            Console.WriteLine(fullLog);
            _output.WriteLine(fullLog);

            var outputDirectory = GetTestOutputDirectory();
            var pipelineLogPath = Path.Combine(outputDirectory, "pipeline_full_test_result.txt");
            await File.WriteAllTextAsync(pipelineLogPath, fullLog);

            Assert.False(string.IsNullOrWhiteSpace(validatedResponse.NarrativeText));
        }

        private static string GetTestOutputDirectory()
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (current != null)
            {
                var testPath = Path.Combine(current.FullName, "backend", "tests");
                if (Directory.Exists(testPath))
                {
                    var outputDir = Path.Combine(testPath, "test_output");
                    Directory.CreateDirectory(outputDir);
                    return outputDir;
                }
                current = current.Parent;
            }

            var fallback = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_output");
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        private static PromptBuilder CreatePromptBuilder()
        {
            var current = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (current != null)
            {
                var aiStoryPath = Path.Combine(current.FullName, "backend", "src", "GameBackend.Core", "AIStory");
                if (Directory.Exists(aiStoryPath))
                {
                    return new PromptBuilder(aiStoryPath);
                }
                current = current.Parent;
            }
            return new PromptBuilder(Directory.GetCurrentDirectory());
        }

        private sealed class FakeStoryRepository : GameBackend.Core.Repositories.Interfaces.IStoryRepository
        {
            public Task<StorySession?> GetSessionByIdAsync(string sessionId) => Task.FromResult<StorySession?>(null);
            public Task<StorySession?> GetSessionByCharacterIdAsync(string characterId) => Task.FromResult<StorySession?>(null);
            public Task SaveSessionAsync(StorySession session) => Task.CompletedTask;
            public Task SaveActionAsync(StoryAction action) => Task.CompletedTask;
            public Task<List<StoryAction>> GetActionsBySessionIdAsync(string sessionId) => Task.FromResult(new List<StoryAction>());
        }

        private sealed class FakeCharacterService : GameBackend.Core.Services.Interfaces.ICharacterService
        {
            public Task<CharacterResponse> GetCharacterAsync(string characterId) => Task.FromResult(new CharacterResponse());
            public Task<CharacterResponse> CreateCharacterAsync(CreateCharacterRequest request) => Task.FromResult(new CharacterResponse());
            public Task<Character> ApplyExperienceAndLevelUp(Character character, int expGained) => Task.FromResult(character);
            public CharacterStats CalculateEffectiveStats(Character character, IEnumerable<Inventory> equippedItems, IDictionary<string, Item> itemLookup) => new CharacterStats();
            public Task EnsureAliveOrAutoReviveAsync(string characterId) => Task.CompletedTask;
        }

        private sealed class FakeInventoryRepository : GameBackend.Core.Repositories.Interfaces.IInventoryRepository
        {
            public Task<List<Inventory>> GetByCharacterIdAsync(string characterId) => Task.FromResult(new List<Inventory>());
            public Task<Inventory?> GetByInventoryIdAsync(string inventoryId) => Task.FromResult<Inventory?>(null);
            public Task<Inventory?> FindByCharacterAndItemAsync(string characterId, string itemId) => Task.FromResult<Inventory?>(null);
            public Task<List<Inventory>> GetEquippedItemsAsync(string characterId) => Task.FromResult(new List<Inventory>());
            public Task<int> CountSlotsAsync(string characterId) => Task.FromResult(0);
            public Task SaveAsync(Inventory inventory) => Task.CompletedTask;
            public Task DeleteAsync(string inventoryId) => Task.CompletedTask;
        }

        private sealed class FakeBattleRepository : GameBackend.Core.Repositories.Interfaces.IBattleRepository
        {
            public Task<BossEncounter?> GetEncounterByIdAsync(string encounterId) => Task.FromResult<BossEncounter?>(null);
            public Task SaveEncounterAsync(BossEncounter encounter) => Task.CompletedTask;
            public Task<Battle?> GetBattleByIdAsync(string battleId) => Task.FromResult<Battle?>(null);
            public Task SaveBattleAsync(Battle battle) => Task.CompletedTask;
            public Task SaveLootDropAsync(LootDrop lootDrop) => Task.CompletedTask;
        }

        private sealed class FakeBossRepository : GameBackend.Core.Repositories.Interfaces.IBossRepository
        {
            public Task<Boss?> GetByIdAsync(string bossId) => Task.FromResult<Boss?>(null);
            public Task SaveAsync(Boss boss) => Task.CompletedTask;
        }
    }
}
