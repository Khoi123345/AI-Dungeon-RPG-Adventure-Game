using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using GameShared.DTOs.Story;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services.Parsing
{
    public class FlexibleStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                return doc.RootElement.ToString();
            }
            if (reader.TokenType == JsonTokenType.True) return "true";
            if (reader.TokenType == JsonTokenType.False) return "false";
            if (reader.TokenType == JsonTokenType.Null) return null;
            return reader.GetString();
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

    public static class StoryAiResponseParser
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Converters = { new FlexibleStringConverter() }
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
                    if (parsed != null && !string.IsNullOrWhiteSpace(parsed.NarrativeText))
                    {
                        return ApplyDefaults(parsed, session, rawResponse, defaultActionType);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to parse StoryAiResponse JSON, falling back to regex/clean extraction");
                }
            }

            var (narrativeText, fallbackChoices, triggerBattle, bossId, bossName) = ExtractFallbackFields(rawResponse);

            return new StoryAiResponse
            {
                NarrativeText = narrativeText,
                CurrentNodeId = session.currentNodeId,
                CurrentLocation = session.currentLocation,
                CurrentChapterId = session.currentChapterId,
                StorySummary = session.storySummary,
                ActionType = defaultActionType,
                MetadataJson = "{}",
                CharacterDelta = new StoryAiCharacterDelta(),
                InventoryChanges = new List<StoryAiInventoryChange>(),
                Choices = fallbackChoices,
                TriggerBattle = triggerBattle,
                BossId = bossId,
                BossName = bossName
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

            trimmed = trimmed.Trim();

            int firstBrace = trimmed.IndexOf('{');
            int lastBrace = trimmed.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                trimmed = trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            return trimmed;
        }

        private static (string narrativeText, List<StoryChoiceOption> choices, bool triggerBattle, string bossId, string bossName) ExtractFallbackFields(string rawResponse)
        {
            string narrative = "Sương mù che khuất tầm nhìn, bạn cảm thấy có một thực thể bí ẩn đang can thiệp vào dòng thời gian. (Lỗi kết nối hắc ám)";
            var choicesList = new List<StoryChoiceOption>();
            bool triggerBattle = false;
            string bossId = null;
            string bossName = null;

            if (string.IsNullOrWhiteSpace(rawResponse)) return (narrative, choicesList, triggerBattle, bossId, bossName);

            // Cố gắng lấy narrativeText
            try
            {
                var match = Regex.Match(rawResponse, @"\""narrativeText\""\s*:\s*\""(.*?)\""(?=\s*,\s*\""|\s*\})", RegexOptions.Singleline);
                if (match.Success)
                {
                    narrative = match.Groups[1].Value
                        .Replace("\\\"", "\"")
                        .Replace("\\n", "\n")
                        .Replace("\\r", "");
                }
            }
            catch { }

            // Cố gắng lấy mảng choices
            try
            {
                var choicesMatch = Regex.Match(rawResponse, @"\""choices\""\s*:\s*(\[.*?\])", RegexOptions.Singleline);
                if (choicesMatch.Success)
                {
                    var choicesJson = choicesMatch.Groups[1].Value;
                    var extractedChoices = JsonSerializer.Deserialize<List<StoryChoiceOption>>(choicesJson, Options);
                    if (extractedChoices != null && extractedChoices.Count > 0)
                    {
                        choicesList = extractedChoices;
                    }
                }
            }
            catch { }

            // Cố gắng lấy triggerBattle, bossId, bossName
            try
            {
                var battleMatch = Regex.Match(rawResponse, @"\""triggerBattle\""\s*:\s*(true|false)", RegexOptions.IgnoreCase);
                if (battleMatch.Success && battleMatch.Groups[1].Value.ToLower() == "true")
                {
                    triggerBattle = true;
                }

                var bossIdMatch = Regex.Match(rawResponse, @"\""bossId\""\s*:\s*\""(.*?)\""");
                if (bossIdMatch.Success) bossId = bossIdMatch.Groups[1].Value;

                var bossNameMatch = Regex.Match(rawResponse, @"\""bossName\""\s*:\s*\""(.*?)\""");
                if (bossNameMatch.Success) bossName = bossNameMatch.Groups[1].Value;
            }
            catch { }

            return (narrative, choicesList, triggerBattle, bossId, bossName);
        }
    }
}