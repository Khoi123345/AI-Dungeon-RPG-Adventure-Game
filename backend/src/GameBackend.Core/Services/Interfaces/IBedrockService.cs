namespace GameBackend.Core.Services.Interfaces
{
    /// <summary>
    /// Tích hợp Amazon Bedrock (Claude AI) để sinh narrative cốt truyện.
    /// Có retry logic và fallback khi Bedrock không khả dụng.
    /// </summary>
    public interface IBedrockService
    {
        /// <summary>
        /// Gửi prompt lên Claude AI và nhận narrative response.
        /// Timeout tối đa: 25 giây (dưới ngưỡng API Gateway 29s).
        /// </summary>
        Task<string> GenerateNarrativeAsync(string systemPrompt, string userPrompt);

        /// <summary>
        /// Kiểm tra kết nối Bedrock có khả dụng không.
        /// </summary>
        Task<bool> IsAvailableAsync();
    }
}
