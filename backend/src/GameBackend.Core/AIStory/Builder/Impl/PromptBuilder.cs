using System.IO;
using GameBackend.Core.AIStory.DTOs;

namespace GameBackend.Core.AIStory
{
    public class PromptBuilder : IPromptBuilder
    {
        private readonly string _templatePath;

        public PromptBuilder(string templatePath)
        {
            _templatePath = templatePath;
        }

        public (string SystemPrompt, string UserPrompt) Build(GamePromptContext context)
        {
            string systemPrompt = string.Empty;
            string storyTemplate = string.Empty;

            if (Directory.Exists(_templatePath))
            {
                // Common location: <contentRoot>/Content/Prompt/
                var promptDir = Path.Combine(_templatePath, "Content", "Prompt");
                if (!Directory.Exists(promptDir)) promptDir = _templatePath;

                var systemPath = Path.Combine(promptDir, "system_prompt.md");
                var storyPath = Path.Combine(promptDir, "story_prompt.md");

                if (File.Exists(systemPath)) systemPrompt = File.ReadAllText(systemPath);
                if (File.Exists(storyPath)) storyTemplate = File.ReadAllText(storyPath);
            }
            else if (File.Exists(_templatePath))
            {
                // Legacy: single file passed
                storyTemplate = File.ReadAllText(_templatePath);
            }

            // Fallback default system prompt if file not found
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                systemPrompt = "Bạn là một Game Master (Người Quản Trò) xuất sắc cho trò chơi Text-based RPG mang phong cách Dark Fantasy: \"Aethelgard - Etherea: The Fractured Realm\". Hãy dẫn dắt cốt truyện bằng tiếng Việt.";
            }

            // Fallback user story template if file not found
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

            // Safe default if StorySummary is empty from context (do NOT fallback to summary_prompt.md!)
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
    }
}