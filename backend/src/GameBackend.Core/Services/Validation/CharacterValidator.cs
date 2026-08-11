namespace GameBackend.Core.Services.Validation
{
    public sealed class CharacterValidator : IGameRuleSubValidator
    {
        public Task ValidateAsync(GameRuleValidationContext context)
        {
            var delta = context.Response.CharacterDelta ??= new GameShared.DTOs.Story.StoryAiCharacterDelta();

            // Story AI is narrative-only. Gameplay stats are authoritative and may
            // only be changed by battle, inventory, consumable or revive services.
            delta.HpDelta = 0;
            delta.MpDelta = 0;
            delta.GoldDelta = 0;
            delta.ExpDelta = 0;
            delta.Status = context.Character.status;

            if (string.IsNullOrWhiteSpace(delta.CurrentLocationId))
            {
                delta.CurrentLocationId = context.Response.CurrentLocation ?? context.Session.currentLocation;
            }

            return Task.CompletedTask;
        }
    }
}
