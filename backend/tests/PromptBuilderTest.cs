using System;
using Xunit;
using System.IO;
using Xunit.Abstractions;

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

        var context = new PromptContext
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

        var prompt = builder.Build(context);

        // Display the final prompt for format verification
        Console.WriteLine("\n========== FINAL PROMPT ==========");
        Console.WriteLine(prompt);
        Console.WriteLine("==================================\n");

        // Also write to file for easy viewing
        var outputFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "prompt_output.txt");
        File.WriteAllText(outputFile, prompt);
        Console.WriteLine($"Full prompt written to: {Path.GetFullPath(outputFile)}");

        Assert.NotNull(prompt);

        // system prompt
        Assert.Contains(
            "You are an RPG narrator",
            prompt);

        // replaced values
        Assert.Contains(
            "Dragon World",
            prompt);

        Assert.Contains(
            "Level: 12",
            prompt);

        Assert.Contains(
            "Dragon Sword",
            prompt);

        Assert.Contains(
            "Talk to dragon",
            prompt);

        // no unresolved placeholders
        Assert.DoesNotContain(
            "{{",
            prompt);

        Assert.DoesNotContain(
            "}}",
            prompt);
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
            Path.Combine("backend", "src", "GameBackend.Core", "AIStory")
        };

        foreach (var c in candidates)
        {
            var full = Path.GetFullPath(c);
            if (Directory.Exists(full))
            {
                return new PromptBuilder(full);
            }
        }

        return new PromptBuilder(Directory.GetCurrentDirectory());
    }
}