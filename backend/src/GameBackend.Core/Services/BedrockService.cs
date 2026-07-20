using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using GameBackend.Core.Config;
using GameBackend.Core.Services.Interfaces;
using GameShared.DTOs.Story;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameBackend.Core.Services
{
    /// <summary>
    /// BedrockService kết nối trực tiếp với AWS Bedrock Runtime để gọi Claude AI.
    /// Đây là nơi duy nhất trong hệ thống tương tác với AWS Bedrock.
    /// </summary>
    public class BedrockService : IBedrockService
    {
        private readonly IAmazonBedrockRuntime? _client;
        private readonly BedrockOptions _options;
        private readonly ILogger<BedrockService> _logger;
        private readonly bool _useMockResponses;
        private const int SimulatedLatencyMs = 120;

        public BedrockService(
            IAmazonBedrockRuntime client,
            IOptions<BedrockOptions> options,
            ILogger<BedrockService> logger)
        {
            _client = client;
            _options = options?.Value ?? new BedrockOptions();
            _logger = logger;
            _useMockResponses = string.Equals(
                Environment.GetEnvironmentVariable("BEDROCK_USE_MOCK"),
                "true",
                StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> GenerateNarrativeAsync(string systemPrompt, string userPrompt)
        {
            if (_client != null && !_useMockResponses)
            {
                try
                {
                    var modelId = string.IsNullOrWhiteSpace(_options.ModelId)
                        ? "anthropic.claude-sonnet-4-5-20250929-v1:0"
                        : _options.ModelId;

                    _logger.LogInformation("Calling AWS Bedrock ConverseAsync for model: {ModelId} in region: {Region}", modelId, _options.Region);

                    var converseRequest = new ConverseRequest
                    {
                        ModelId = modelId,
                        InferenceConfig = new InferenceConfiguration
                        {
                            Temperature = _options.Temperature,
                            MaxTokens = _options.MaxTokens > 0 ? _options.MaxTokens : 1000,
                            TopP = _options.TopP
                        },
                        Messages = new List<Message>
                        {
                            new Message
                            {
                                Role = ConversationRole.User,
                                Content = new List<ContentBlock>
                                {
                                    new ContentBlock { Text = userPrompt ?? string.Empty }
                                }
                            }
                        }
                    };

                    if (!string.IsNullOrWhiteSpace(systemPrompt))
                    {
                        converseRequest.System = new List<SystemContentBlock>
                        {
                            new SystemContentBlock { Text = systemPrompt }
                        };
                    }

                    var response = await _client.ConverseAsync(converseRequest);
                    var text = response.Output?.Message?.Content?.FirstOrDefault()?.Text;

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to call AWS Bedrock ConverseAsync. Falling back to mock response.");
                }
            }

            await Task.Delay(SimulatedLatencyMs);
            var narrative = GenerateMockNarrative(systemPrompt, userPrompt);
            _logger.LogInformation("Mock Bedrock response generated for prompt: {Prompt}", Truncate(userPrompt, 80));
            return narrative;
        }

        public Task<bool> IsAvailableAsync()
        {
            return Task.FromResult(_client != null);
        }

        private static string GenerateMockNarrative(string systemPrompt, string userPrompt)
        {
            var templates = new[]
            {
                "Bóng tối trong tàn tích cổ khẽ rung lên như đang thở. Mỗi bước chân của bạn vang lại thành một lời cảnh báo.",
                "Một làn gió lạnh lướt qua hành lang đá, mang theo mùi bụi cũ và nguy hiểm đang chờ phía trước.",
                "Những ký hiệu rune mờ nhạt bừng sáng dưới lòng đất. Cánh cửa trước mặt bạn như đang nhớ ra tên người xâm nhập.",
                "Từ sâu trong bóng đêm, một tiếng gầm trầm thấp vọng đến. Cuộc phiêu lưu vừa chạm vào vùng đất không nên thức tỉnh."
            };

            var hash = HashCode.Combine(systemPrompt ?? string.Empty, userPrompt ?? string.Empty);
            var index = (hash & int.MaxValue) % templates.Length;
            var selected = templates[index];

            var response = new StoryAiResponse
            {
                NarrativeText = selected,
                StorySummary = selected,
                ActionType = TryExtractChoice(userPrompt, out var choiceIndex) ? "choice" : "player_action",
                MetadataJson = "{}",
                TriggerBattle = false,
                BossId = string.Empty,
                CharacterDelta = new StoryAiCharacterDelta()
            };

            if (TryExtractChoice(userPrompt, out var choiceIndex2))
            {
                response = choiceIndex2 switch
                {
                    0 => new StoryAiResponse
                    {
                        NarrativeText = selected + " Bạn lao thẳng về phía trước, sẵn sàng cho một trận chiến sống còn.",
                        StorySummary = selected,
                        ActionType = "choice",
                        MetadataJson = "{}",
                        TriggerBattle = true,
                        BossId = "boss_goblin_chief",
                        BossLevel = 10,
                        CharacterDelta = new StoryAiCharacterDelta { ExpDelta = 25 }
                    },
                    1 => new StoryAiResponse
                    {
                        NarrativeText = selected + " Bạn cúi xuống quan sát dấu vết trên nền đá, tìm kiếm bí mật bị giấu kín.",
                        StorySummary = selected,
                        ActionType = "choice",
                        MetadataJson = "{}",
                        TriggerBattle = false,
                        CharacterDelta = new StoryAiCharacterDelta { GoldDelta = 15, ExpDelta = 15 }
                    },
                    _ => new StoryAiResponse
                    {
                        NarrativeText = selected + " Bạn dừng lại để ổn định hơi thở, chấp nhận một khoảng lặng ngắn trước khi bước tiếp.",
                        StorySummary = selected,
                        ActionType = "choice",
                        MetadataJson = "{}",
                        TriggerBattle = false,
                        CharacterDelta = new StoryAiCharacterDelta { HpDelta = 18, GoldDelta = -5, ExpDelta = 5 }
                    }
                };
            }

            return JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }

        private static bool TryExtractChoice(string userPrompt, out int choiceIndex)
        {
            choiceIndex = -1;

            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                return false;
            }

            var marker = "choice ";
            var markerIndex = userPrompt.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex < 0)
            {
                return false;
            }

            var startIndex = markerIndex + marker.Length;
            var digits = new string(userPrompt[startIndex..].TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(digits, out choiceIndex);
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            return value[..maxLength] + "...";
        }
    }
}
