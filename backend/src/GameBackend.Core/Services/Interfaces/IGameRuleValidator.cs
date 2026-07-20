using GameShared.DTOs.Story;
using GameShared.Models;

namespace GameBackend.Core.Services.Interfaces
{
    public interface IGameRuleValidator
    {
        Task<StoryAiResponse> ValidateAndSanitizeAsync(StorySession session, Character character, StoryAiResponse aiResponse);
    }
}
