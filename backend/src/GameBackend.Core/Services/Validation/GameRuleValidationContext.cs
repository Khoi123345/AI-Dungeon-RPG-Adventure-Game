using GameShared.DTOs.Story;
using GameShared.Models;

namespace GameBackend.Core.Services.Validation
{
    public sealed class GameRuleValidationContext
    {
        public required StorySession Session { get; init; }

        public required Character Character { get; init; }

        public required StoryAiResponse Response { get; init; }
    }
}
