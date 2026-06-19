using System;
using System.IO;

public class PromptBuilder
{
    private readonly string _templatePath;

    public PromptBuilder(string templatePath)
    {
        _templatePath = templatePath;
    }

    public string Build(PromptContext context)
    {
        string systemPrompt = string.Empty;
        string storyTemplate = string.Empty;
        string summaryTemplate = string.Empty;

        if (Directory.Exists(_templatePath))
        {
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
            storyTemplate = File.ReadAllText(_templatePath);
        }

        if (string.IsNullOrWhiteSpace(storyTemplate))
        {
            storyTemplate =
                "SYSTEM PROMPT\n-------------\nYou are an RPG narrator.\n\n" +
                "WORLD\n-------------\n{{world}}\n\n" +
                "CHARACTER\n-------------\n{{character}}\n\n" +
                "INVENTORY\n-------------\n{{inventory}}\n\n" +
                "CHAPTER\n-------------\n{{chapter}}\n\n" +
                "LOCATION\n-------------\n{{location}}\n\n" +
                "STORY SUMMARY\n-------------\n{{summary}}\n\n" +
                "RECENT TURNS\n-------------\n{{recentTurns}}\n\n" +
                "CURRENT ACTION\n-------------\n{{action}}";
        }

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
                     .Replace("{{action}}", context.UserAction ?? string.Empty)
                     // Mock placeholders from story_prompt.md
                     .Replace("{{current_location_name}}", "Mock Location")
                     .Replace("{{current_location_lore}}", "Mock Lore")
                     .Replace("{{current_quest_description}}", "Mock Quest")
                     .Replace("{{player_hp}}", "100")
                     .Replace("{{player_max_hp}}", "100")
                     .Replace("{{player_level}}", "1")
                     .Replace("{{player_inventory}}", "Mock Inventory")
                     .Replace("{{defeated_bosses}}", "None")
                     .Replace("{{recent_story_summary}}", "Mock Summary")
                     .Replace("{{system_injected_event}}", "")
                     .Replace("{{player_action}}", "")
                     .Replace("{{boss_name}}", "Mock Boss");

        return final;
    }
}
