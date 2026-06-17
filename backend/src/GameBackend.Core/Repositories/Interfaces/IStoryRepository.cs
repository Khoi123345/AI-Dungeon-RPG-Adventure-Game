using GameShared.Models;

namespace GameBackend.Core.Repositories.Interfaces
{
    public interface IStoryRepository
    {
        Task<StorySession?> GetSessionByIdAsync(string sessionId);
        Task<StorySession?> GetSessionByCharacterIdAsync(string characterId);
        Task SaveSessionAsync(StorySession session);
        Task SaveActionAsync(StoryAction action);
        Task<List<StoryAction>> GetActionsBySessionIdAsync(string sessionId);
    }
}
