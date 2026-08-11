using GameBackend.Core.AIStory.DTOs;

namespace GameBackend.Core.AIStory
{
    /// <summary>
    /// Xây dựng SystemPrompt và UserPrompt cho Bedrock AI từ GamePromptContext.
    /// Prompt templates được load qua IPromptLoader (S3 hoặc filesystem tùy môi trường).
    ///
    /// Fallback: nếu loader trả về empty string, dùng template hardcoded mặc định.
    /// </summary>
    public class PromptBuilder : IPromptBuilder
    {
        private readonly IPromptLoader _loader;

        public PromptBuilder(IPromptLoader loader)
        {
            _loader = loader ?? throw new ArgumentNullException(nameof(loader));
        }

        /// <summary>
        /// Build prompt bất đồng bộ — dùng async vì S3PromptLoader cần await.
        /// </summary>
        public async Task<(string SystemPrompt, string UserPrompt)> BuildAsync(GamePromptContext context)
        {
            var systemPrompt = await _loader.GetSystemPromptAsync();
            var storyTemplate = await _loader.GetStoryPromptAsync();

            // Fallback default system prompt nếu file không tìm thấy
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                systemPrompt = "Bạn là một Game Master (Người Quản Trò) xuất sắc cho trò chơi Text-based RPG mang phong cách Dark Fantasy: \"Aethelgard - Etherea: The Fractured Realm\". Hãy dẫn dắt cốt truyện bằng tiếng Việt.";
            }

            // Fallback user story template nếu file không tìm thấy
            if (string.IsNullOrWhiteSpace(storyTemplate))
            {
                storyTemplate =
                    "WORLD\n-------------\n{{world}}\n\n" +
                    "CHARACTER\n-------------\n{{character}}\n\n" +
                    "INVENTORY\n-------------\n{{inventory}}\n\n" +
                    "CHAPTER\n-------------\n{{chapter}}\n\n" +
                    "CURRENT LOCATION\n----------------\n{{location}}\n\n" +
                    "STORY SUMMARY\n-------------\n{{summary}}\n\n" +
                    "RECENT TURNS\n-------------\n{{recentTurns}}\n\n" +
                    "CURRENT ACTION\n-------------\n{{action}}";
            }

            // Safe default nếu StorySummary rỗng (KHÔNG fallback sang summary_prompt.md!)
            var summaryValue = !string.IsNullOrWhiteSpace(context.StorySummary)
                ? context.StorySummary
                : "Chưa có tóm tắt trước đó.";

            var userPrompt = storyTemplate
                .Replace("{{world}}", context.World ?? string.Empty)
                .Replace("{{character}}", context.CharacterInfo ?? string.Empty)
                .Replace("{{inventory}}", context.InventoryInfo ?? string.Empty)
                .Replace("{{chapter}}", context.Chapter ?? string.Empty)
                .Replace("{{location}}", context.Location ?? string.Empty)
                .Replace("{{summary}}", summaryValue)
                .Replace("{{recentTurns}}", context.RecentTurns ?? string.Empty)
                .Replace("{{action}}", context.UserAction ?? string.Empty)
                .Replace("{{system_injected_event}}", context.SystemInjectedEvent ?? string.Empty)
                .Replace("{{defeated_bosses}}", context.DefeatedBossesInfo ?? "Chưa tiêu diệt Boss nào.");

            return (systemPrompt, userPrompt);
        }

        /// <summary>
        /// Build synchronous (backward compat) — gọi BuildAsync().GetAwaiter().GetResult().
        /// Dùng cho code cũ chưa migrate sang async. Không nên dùng trong hot path.
        /// </summary>
        public (string SystemPrompt, string UserPrompt) Build(GamePromptContext context)
        {
            return BuildAsync(context).GetAwaiter().GetResult();
        }
    }
}