namespace GameBackend.Core.Services.Validation
{
    /// <summary>
    /// Validates quest-related narrative constraints in the AI story response,
    /// such as capping narrative text length to prevent oversized outputs.
    /// </summary>
    public sealed class QuestValidator : IGameRuleSubValidator
    {
        private const int MaxNarrativeLength = 2000;

        public Task ValidateAsync(GameRuleValidationContext context)
        {
            var response = context.Response;

            // Clamp narrative text to a safe maximum length
            if (!string.IsNullOrEmpty(response.NarrativeText) &&
                response.NarrativeText.Length > MaxNarrativeLength)
            {
                response.NarrativeText = response.NarrativeText[..MaxNarrativeLength];
            }

            // Ensure ActionType is never null to avoid downstream null-reference issues
            if (string.IsNullOrWhiteSpace(response.ActionType))
            {
                response.ActionType = "player_action";
            }

            return Task.CompletedTask;
        }
    }
}
