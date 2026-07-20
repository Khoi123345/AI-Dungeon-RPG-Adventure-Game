using GameShared.DTOs.Story;
using GameShared.Models;

namespace GameBackend.Core.Services.Interfaces
{
    public interface IStoryStateUpdater
    {
        Task ApplyAsync(StorySession session, Character character, StoryAiResponse aiResponse);
    }
}