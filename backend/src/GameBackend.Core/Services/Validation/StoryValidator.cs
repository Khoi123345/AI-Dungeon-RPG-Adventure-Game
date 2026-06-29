using System.Text.RegularExpressions;

namespace GameBackend.Core.Services.Validation
{
    public sealed class StoryValidator : IGameRuleSubValidator
    {
        private static readonly string[] DeathKeywords =
        {
            "dragon chết",
            "đã chết",
            "bị tiêu diệt",
            "slain",
            "defeated"
        };

        public Task ValidateAsync(GameRuleValidationContext context)
        {
            var response = context.Response;
            response.NarrativeText ??= string.Empty;

            if (response.TriggerBattle)
            {
                return Task.CompletedTask;
            }

            var sentences = Regex.Split(response.NarrativeText, @"(?<=[.!?])\s+");
            var filtered = sentences
                .Where(sentence => !ContainsDeathKeyword(sentence))
                .ToList();

            if (filtered.Count == 0)
            {
                response.NarrativeText = "Bóng tối rung chuyển, một mối đe dọa vẫn đang ẩn mình.";
                return Task.CompletedTask;
            }

            response.NarrativeText = string.Join(" ", filtered).Trim();
            return Task.CompletedTask;
        }

        private static bool ContainsDeathKeyword(string sentence)
        {
            return DeathKeywords.Any(keyword => sentence.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }
}
