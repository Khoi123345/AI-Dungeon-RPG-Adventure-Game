using System.Threading.Tasks;

namespace GameBackend.Core.AIStory.Services
{
    public interface IContentService
    {
        Task<string> GetWorldAsync();

        Task<string> GetChapterAsync(string chapterId);

        Task<string> GetLocationAsync(string locationId);
    }
}