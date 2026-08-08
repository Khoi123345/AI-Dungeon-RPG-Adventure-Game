using System.Text.Json;
using GameShared.DTOs.Story;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services.Parsing
{
    public static class StoryAiResponseParser
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };

        public static StoryAiResponse Parse(string rawResponse, StorySession session, string defaultActionType, ILogger? logger = null)
        {
            if (!string.IsNullOrWhiteSpace(rawResponse))
            {
                logger?.LogInformation("Raw AI Response: {RawResponse}", rawResponse);
                try
                {
                    var cleaned = CleanJsonResponse(rawResponse);
                    var parsed = JsonSerializer.Deserialize<StoryAiResponse>(cleaned, Options);
                    if (parsed != null)
                    {
                        return ApplyDefaults(parsed, session, rawResponse, defaultActionType);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to parse StoryAiResponse JSON, falling back to default wrapper");
                }
            }

            return new StoryAiResponse
            {
                NarrativeText = rawResponse,
                CurrentNodeId = session.currentNodeId,
                CurrentLocation = session.currentLocation,
                CurrentChapterId = session.currentChapterId,
                StorySummary = session.storySummary,
                ActionType = defaultActionType,
                MetadataJson = "{}",
                CharacterDelta = new StoryAiCharacterDelta(),
                InventoryChanges = new List<StoryAiInventoryChange>()
            };
        }

        public static string Serialize(StoryAiResponse response)
        {
            return JsonSerializer.Serialize(response, Options);
        }

        private static StoryAiResponse ApplyDefaults(StoryAiResponse response, StorySession session, string rawResponse, string defaultActionType)
        {
            response.NarrativeText ??= rawResponse;
            response.CurrentNodeId ??= session.currentNodeId;
            response.CurrentLocation ??= session.currentLocation;
            response.CurrentChapterId ??= session.currentChapterId;
            response.StorySummary ??= session.storySummary;
            response.ActionType ??= defaultActionType;
            response.MetadataJson ??= "{}";
            response.CharacterDelta ??= new StoryAiCharacterDelta();
            response.InventoryChanges ??= new List<StoryAiInventoryChange>();
            return response;
        }

        private static string CleanJsonResponse(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var trimmed = input.Trim();
            if (trimmed.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[7..];
            }
            else if (trimmed.StartsWith("```"))
            {
                trimmed = trimmed[3..];
            }

            if (trimmed.EndsWith("```"))
            {
                trimmed = trimmed[..^3];
            }

            return trimmed.Trim();
        }
    }
}