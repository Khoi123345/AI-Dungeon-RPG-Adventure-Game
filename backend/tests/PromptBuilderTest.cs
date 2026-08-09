using System;
using Xunit;
using System.IO;
using Xunit.Abstractions;
using GameBackend.Core.AIStory;
using GameBackend.Core.AIStory.DTOs;

public class PromptBuilderTests
{
    private readonly ITestOutputHelper _output;

    public PromptBuilderTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Build_Should_Generate_Complete_Prompt()
    {
        var builder = CreatePromptBuilder();

        var context = new GamePromptContext
        {
            World = "Dragon World",
            CharacterInfo = "Level: 12",
            InventoryInfo = "Dragon Sword",
            Chapter = "Chapter 1",
            Location = "Ancient Cave",
            StorySummary = "Killed Goblin King",
            RecentTurns = "User: Open chest",
            UserAction = "Talk to dragon"
        };

        var (systemPrompt, userPrompt) = builder.Build(context);

        // Display the final prompt for format verification
        Console.WriteLine("\n========== SYSTEM PROMPT ==========");
        Console.WriteLine(systemPrompt);
        Console.WriteLine("\n========== USER PROMPT ==========");
        Console.WriteLine(userPrompt);
        Console.WriteLine("==================================\n");

        Assert.NotNull(systemPrompt);
        Assert.NotNull(userPrompt);

        // system prompt should contain Game Master instructions
        Assert.Contains(
            "Game Master",
            systemPrompt);

        // user prompt replaced values
        Assert.Contains(
            "Dragon World",
            userPrompt);

        Assert.Contains(
            "Level: 12",
            userPrompt);

        Assert.Contains(
            "Dragon Sword",
            userPrompt);

        Assert.Contains(
            "Talk to dragon",
            userPrompt);

        // no unresolved placeholders
        Assert.DoesNotContain(
            "{{",
            userPrompt);

        Assert.DoesNotContain(
            "}}",
            userPrompt);
    }

    private PromptBuilder CreatePromptBuilder()
    {
        // Walk up from current directory to find repo root (look for backend/src/GameBackend.Core/AIStory)
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

        // Try several candidate locations relative to test execution directory
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "..", "src", "GameBackend.Core", "AIStory"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "src", "GameBackend.Core", "AIStory"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "src", "GameBackend.Core", "AIStory"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "src", "GameBackend.Core", "AIStory")
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
            {
                return new PromptBuilder(candidate);
            }
        }

        return new PromptBuilder(Directory.GetCurrentDirectory());
    }
}