using System.Threading.Tasks;

namespace GameBackend.Core.AIStory.Services
{
    public interface IContentService
    {
        Task<string> GetWorldAsync();

        Task<string> GetChapterAsync(string chapterId);

        Task<string> GetLocationAsync(string locationId);

        Task<string> GetBossAsync(string bossId);

        Task<string> GetItemAsync(string itemId);

        Task<string> GetQuestAsync(string questId);

        Task<bool> BossExistsAsync(string bossId);

        Task<bool> ItemExistsAsync(string itemId);

        Task<bool> LocationExistsAsync(string locationId);

        Task<bool> QuestExistsAsync(string questId);
    }
}