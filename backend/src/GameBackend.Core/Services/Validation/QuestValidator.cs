namespace GameBackend.Core.Services.Validation
{
    public sealed class QuestValidator : IGameRuleSubValidator
    {
        private static readonly HashSet<string> AllowedActionTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "opening",
            "choice",
            "player_action"
        };

        public Task ValidateAsync(GameRuleValidationContext context)
        {
            var response = context.Response;

            if (string.IsNullOrWhiteSpace(response.ActionType) || !AllowedActionTypes.Contains(response.ActionType))
            {
                response.ActionType = "player_action";
            }

            response.CurrentNodeId = string.IsNullOrWhiteSpace(response.CurrentNodeId)
                ? context.Session.currentNodeId
                : response.CurrentNodeId;

            response.CurrentChapterId = string.IsNullOrWhiteSpace(response.CurrentChapterId)
                ? context.Session.currentChapterId
                : response.CurrentChapterId;

            response.StorySummary = string.IsNullOrWhiteSpace(response.StorySummary)
                ? context.Session.storySummary
                : response.StorySummary;

            if (response.StorySummary.Length > 500)
            {
                response.StorySummary = response.StorySummary[..500];
            }

            return Task.CompletedTask;
        }
    }
}
