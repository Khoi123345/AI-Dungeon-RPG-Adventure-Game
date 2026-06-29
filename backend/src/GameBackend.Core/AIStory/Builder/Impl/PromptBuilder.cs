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

        public string Build(GamePromptContext context)
        {
            // Support passing either a file path (legacy) or a directory that contains the Prompt files.
            string systemPrompt = string.Empty;
            string storyTemplate = string.Empty;
            string summaryTemplate = string.Empty;

            if (Directory.Exists(_templatePath))
            {
                // Common location: <contentRoot>/Content/Prompt/
                var promptDir = Path.Combine(_templatePath, "Content", "Prompt");
                if (!Directory.Exists(promptDir)) promptDir = _templatePath;

                var systemPath = Path.Combine(promptDir, "system_prompt.md");
                var storyPath = Path.Combine(promptDir, "story_prompt.md");
                var summaryPath = Path.Combine(promptDir, "summary_prompt.md");

                if (File.Exists(systemPath)) systemPrompt = File.ReadAllText(systemPath);
                if (File.Exists(storyPath)) storyTemplate = File.ReadAllText(storyPath);
                if (File.Exists(summaryPath)) summaryTemplate = File.ReadAllText(summaryPath);
            }
            else if (File.Exists(_templatePath))
            {
                // legacy: single file passed
                storyTemplate = File.ReadAllText(_templatePath);
            }

            // If no story template found, combine available pieces naively
            if (string.IsNullOrWhiteSpace(storyTemplate))
            {
                // fallback: build a template in the expected structure
                storyTemplate =
                    "WORLD\n-------------\n{{world}}\n\n" +
                    "CHARACTER\n-------------\n{{character}}\n\n" +
                    "INVENTORY\n-------------\n{{inventory}}\n\n" +
                    "CHAPTER\n-------------\n{{chapter}}\n\n" +
                    "LOCATION\n-------------\n{{location}}\n\n" +
                    "STORY SUMMARY\n-------------\n{{summary}}\n\n" +
                    "RECENT TURNS\n-------------\n{{recentTurns}}\n\n" +
                    "CURRENT ACTION\n-------------\n{{action}}";
            }

            // If summary missing from context, prefer summary template file if available
            var summaryValue = !string.IsNullOrWhiteSpace(context.StorySummary)
                ? context.StorySummary
                : summaryTemplate ?? string.Empty;

            var final = string.IsNullOrWhiteSpace(systemPrompt)
                ? storyTemplate
                : (systemPrompt + "\n\n" + storyTemplate);

            final = final.Replace("{{world}}", context.World ?? string.Empty)
                         .Replace("{{character}}", context.CharacterInfo ?? string.Empty)
                         .Replace("{{inventory}}", context.InventoryInfo ?? string.Empty)
                         .Replace("{{chapter}}", context.Chapter ?? string.Empty)
                         .Replace("{{location}}", context.Location ?? string.Empty)
                         .Replace("{{summary}}", summaryValue ?? string.Empty)
                         .Replace("{{recentTurns}}", context.RecentTurns ?? string.Empty)
                         .Replace("{{action}}", context.UserAction ?? string.Empty);

            return final;
        }
    }
}