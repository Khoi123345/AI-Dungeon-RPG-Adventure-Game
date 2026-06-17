using GameBackend.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
    /// <summary>
    /// Tích hợp Amazon Bedrock (Claude) để sinh narrative cốt truyện.
    /// Có retry logic (exponential backoff) và fallback response khi Bedrock không khả dụng.
    /// </summary>
    public class BedrockService : IBedrockService
    {
        private readonly ILogger<BedrockService> _logger;
        private const int MaxRetries = 3;
        private const int BaseDelayMs = 500;

        public BedrockService(ILogger<BedrockService> logger)
        {
            _logger = logger;
        }

        public async Task<string> GenerateNarrativeAsync(string systemPrompt, string userPrompt)
        {
            int retryCount = 0;

            while (retryCount < MaxRetries)
            {
                try
                {
                    // TODO: Thay thế bằng AWS SDK Bedrock Runtime call thực tế
                    // var client = new AmazonBedrockRuntimeClient();
                    // var request = new InvokeModelRequest { ... };
                    // var response = await client.InvokeModelAsync(request);

                    _logger.LogInformation("Bedrock AI call attempt {Attempt} for prompt: {Prompt}", retryCount + 1, userPrompt[..Math.Min(50, userPrompt.Length)]);

                    // Placeholder: trả về fallback response cho đến khi cấu hình Bedrock thực tế
                    await Task.Delay(100); // Simulate network latency
                    return GenerateFallbackNarrative(userPrompt);
                }
                catch (Exception ex) when (retryCount < MaxRetries - 1)
                {
                    retryCount++;
                    int delay = BaseDelayMs * (int)Math.Pow(2, retryCount);
                    _logger.LogWarning(ex, "Bedrock call failed, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})", delay, retryCount, MaxRetries);
                    await Task.Delay(delay);
                }
            }

            _logger.LogError("Bedrock exhausted all retries, returning fallback narrative");
            return GenerateFallbackNarrative(userPrompt);
        }

        public Task<bool> IsAvailableAsync()
        {
            // TODO: Implement health check khi có Bedrock client thực tế
            // Hiện tại trả false để luôn dùng fallback
            return Task.FromResult(false);
        }

        /// <summary>
        /// Fallback narrative templates khi Bedrock không khả dụng.
        /// Đảm bảo game không bao giờ bị treo vì AI service.
        /// </summary>
        private static string GenerateFallbackNarrative(string context)
        {
            var templates = new[]
            {
                "Bóng tối dày đặc bao phủ con đường phía trước. Bạn siết chặt vũ khí và bước tiếp.",
                "Một luồng gió lạnh thổi qua hành lang đá. Tiếng vang từ xa báo hiệu nguy hiểm đang đến gần.",
                "Ánh sáng yếu ớt từ những viên đá rune chiếu lên khuôn mặt bạn. Con đường chia thành nhiều nhánh.",
                "Tiếng rì rào bí ẩn vang lên từ sâu trong tàn tích. Bạn cảm nhận được sức mạnh cổ đại đang thức giấc."
            };

            int index = Math.Abs(context.GetHashCode()) % templates.Length;
            return templates[index];
        }
    }
}
