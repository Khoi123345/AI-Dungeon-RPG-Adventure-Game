using System;
using System.IO;
using System.Text.Json;
using GameBackend.Core.Services.Parsing;
using GameShared.Models;
using Xunit;
using Xunit.Abstractions;


public class StoryAiResponseParserTests
{
    private readonly ITestOutputHelper _output;

    public StoryAiResponseParserTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Parse_Should_Read_Valid_Json()
    {
        var session = CreateSession();
        var rawJson = """
        {
          "narrativeText": "Hahaaha",
          "currentNodeId": "ancient_gate_open",
          "currentLocation": "ancient_cave_depths",
          "currentChapterId": "chapter_2",
          "storySummary": "Player opened the ancient door",
          "actionType": "player_action",
          "metadataJson": "{}",
          "triggerBattle": true,
          "bossId": "boss_goblin_chief",
          "bossName": "Goblin Chief",
          "bossLevel": 10,
          "characterDelta": {
            "hpDelta": -12,
            "goldDelta": 5,
            "expDelta": 25,
            "mpDelta": -3,
            "status": "Alive",
            "currentLocationId": "ancient_cave_depths"
          },
          "inventoryChanges": [
            {
              "itemId": "ancient_key",
              "itemName": "Ancient Key",
              "quantityDelta": 1,
              "equipped": false,
              "slotIndex": 2,
              "locked": false
            }
          ]
        }
        """;

        var response = StoryAiResponseParser.Parse(rawJson, session, "player_action");

        var responseJson = StoryAiResponseParser.Serialize(response);
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        var prettyJson = JsonSerializer.Serialize(
            JsonSerializer.Deserialize<object>(responseJson),
            options);
        Console.WriteLine("\n========== PARSED RESPONSE (Valid JSON) ==========");
        Console.WriteLine(prettyJson);
        Console.WriteLine("==================================================\n");
        
        var outputFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "storyairesponse_valid_output.txt");
        File.WriteAllText(outputFile, prettyJson);
        _output.WriteLine($"Valid JSON response written to: {Path.GetFullPath(outputFile)}");

        Assert.Equal("Hahaaha", response.NarrativeText);
        Assert.Equal("ancient_gate_open", response.CurrentNodeId);
        Assert.Equal("ancient_cave_depths", response.CurrentLocation);
        Assert.Equal("chapter_2", response.CurrentChapterId);
        Assert.Equal("Player opened the ancient door", response.StorySummary);
        Assert.True(response.TriggerBattle);
        Assert.Equal("boss_goblin_chief", response.BossId);
        Assert.Equal(10, response.BossLevel);
        Assert.NotNull(response.CharacterDelta);
        Assert.Equal(-12, response.CharacterDelta.HpDelta);
        Assert.Single(response.InventoryChanges);
        Assert.Equal("ancient_key", response.InventoryChanges[0].ItemId);
    }

    [Fact]
    public void Parse_Should_Fallback_When_Invalid_Json()
    {
        var session = CreateSession();

        var response = StoryAiResponseParser.Parse("this is not json", session, "choice");

        var responseJson = StoryAiResponseParser.Serialize(response);
        
        var options = new JsonSerializerOptions { WriteIndented = true };
        var prettyJson = JsonSerializer.Serialize(
            JsonSerializer.Deserialize<object>(responseJson),
            options);
        Console.WriteLine("\n========== PARSED RESPONSE (Fallback Invalid JSON) ==========");
        Console.WriteLine(prettyJson);
        Console.WriteLine("==============================================================\n");
        
        var outputFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "storyairesponse_fallback_output.txt");
        File.WriteAllText(outputFile, prettyJson);
        _output.WriteLine($"Fallback JSON response written to: {Path.GetFullPath(outputFile)}");

        Assert.Equal("this is not json", response.NarrativeText);
        Assert.Equal(session.currentNodeId, response.CurrentNodeId);
        Assert.Equal(session.currentLocation, response.CurrentLocation);
        Assert.Equal(session.currentChapterId, response.CurrentChapterId);
        Assert.Equal(session.storySummary, response.StorySummary);
        Assert.Equal("choice", response.ActionType);
        Assert.NotNull(response.CharacterDelta);
        Assert.Empty(response.InventoryChanges);
    }

    private static StorySession CreateSession()
    {
        return new StorySession
        {
            sessionId = "session-1",
            characterId = "char-1",
            currentLocation = "ancient_cave",
            currentNodeId = "intro",
            currentChapterId = "chapter_1",
            status = "Active",
            updatedAt = DateTime.UtcNow,
            storyVersion = "1.0",
            storySummary = "Player defeated Goblin King",
            sourceType = "AI"
        };
    }
}
