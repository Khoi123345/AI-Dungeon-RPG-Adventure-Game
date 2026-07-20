namespace GameBackend.Core.Services.Validation
{
    public sealed class CharacterValidator : IGameRuleSubValidator
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Alive",
            "Dead",
            "Injured"
        };

        public Task ValidateAsync(GameRuleValidationContext context)
        {
            var delta = context.Response.CharacterDelta ??= new GameShared.DTOs.Story.StoryAiCharacterDelta();

            delta.HpDelta = Math.Clamp(delta.HpDelta, -200, 50);
            delta.MpDelta = Math.Clamp(delta.MpDelta, -100, 40);
            delta.GoldDelta = Math.Clamp(delta.GoldDelta, -500, 1000);
            delta.ExpDelta = Math.Clamp(delta.ExpDelta, 0, 500);

            if (!string.IsNullOrWhiteSpace(delta.Status) && !AllowedStatuses.Contains(delta.Status))
            {
                delta.Status = context.Character.status;
            }

            if (string.IsNullOrWhiteSpace(delta.CurrentLocationId))
            {
                delta.CurrentLocationId = context.Response.CurrentLocation ?? context.Session.currentLocation;
            }

            return Task.CompletedTask;
        }
    }
}
