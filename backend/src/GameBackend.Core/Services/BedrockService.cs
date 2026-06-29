using GameBackend.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
    /// <summary>
    /// Mock Bedrock service cho giai đoạn chưa kết nối AWS thật.
    /// Có thể tắt mock bằng biến môi trường BEDROCK_USE_MOCK=false.
    /// </summary>
    public class BedrockService : IBedrockService
    {
        private readonly ILogger<BedrockService> _logger;
        private readonly bool _useMockResponses;
        private const int SimulatedLatencyMs = 120;

        public BedrockService(ILogger<BedrockService> logger)
        {
            _logger = logger;
            _useMockResponses = !string.Equals(
                Environment.GetEnvironmentVariable("BEDROCK_USE_MOCK"),
                "false",
                StringComparison.OrdinalIgnoreCase);
        }

        public async Task<string> GenerateNarrativeAsync(string systemPrompt, string userPrompt)
        {
            if (!_useMockResponses)
            {
                _logger.LogWarning("BEDROCK_USE_MOCK is disabled, but real AWS integration is not wired yet. Falling back to mock response.");
            }

            await Task.Delay(SimulatedLatencyMs);

            var narrative = GenerateMockNarrative(systemPrompt, userPrompt);
            _logger.LogInformation("Mock Bedrock response generated for prompt: {Prompt}", Truncate(userPrompt, 80));
            return narrative;
        }

        public Task<bool> IsAvailableAsync()
        {
            return Task.FromResult(true);
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

            if (TryExtractChoice(userPrompt, out var choiceIndex))
            {
                return choiceIndex switch
                {
                    0 => selected + " Bạn lao thẳng về phía trước, sẵn sàng cho một trận chiến sống còn.",
                    1 => selected + " Bạn cúi xuống quan sát dấu vết trên nền đá, tìm kiếm bí mật bị giấu kín.",
                    _ => selected + " Bạn dừng lại để ổn định hơi thở, chấp nhận một khoảng lặng ngắn trước khi bước tiếp."
                };
            }

            return selected;
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
