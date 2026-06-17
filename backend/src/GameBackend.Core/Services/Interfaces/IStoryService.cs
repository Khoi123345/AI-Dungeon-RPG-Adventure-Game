using GameShared.DTOs.Story;

namespace GameBackend.Core.Services.Interfaces
{
    public interface IStoryService
    {
        Task<StoryActionResponse> StartStoryAsync(StoryStartRequest request);
        Task<StoryActionResponse> ProcessActionAsync(StoryActionRequest request);
    }
}
