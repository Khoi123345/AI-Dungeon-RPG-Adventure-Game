using System.IO;
using System.Threading.Tasks;
using GameBackend.Core.AIStory.Services;

namespace GameBackend.Core.AIStory.Services.Impl
{
    public class ContentService : IContentService
    {
        private readonly string _contentRoot;

        public ContentService(string contentRoot)
        {
            _contentRoot = contentRoot;
        }

        public async Task<string> GetWorldAsync()
        {
            return await ReadContentAsync("World", "world");
        }

        public async Task<string> GetChapterAsync(string chapterId)
        {
            return await ReadContentAsync("Chapters", chapterId);
        }

        public async Task<string> GetLocationAsync(string locationId)
        {
            return await ReadContentAsync("Locations", locationId);
        }

        public async Task<string> GetBossAsync(string bossId)
        {
            return await ReadContentAsync("Bosses", bossId);
        }

        public async Task<string> GetItemAsync(string itemId)
        {
            return await ReadContentAsync("Items", itemId);
        }

        public async Task<string> GetQuestAsync(string questId)
        {
            return await ReadContentAsync("Quests", questId);
        }

        public Task<bool> BossExistsAsync(string bossId)
        {
            return Task.FromResult(ContentExists("Bosses", bossId));
        }

        public Task<bool> ItemExistsAsync(string itemId)
        {
            return Task.FromResult(ContentExists("Items", itemId));
        }

        public Task<bool> LocationExistsAsync(string locationId)
        {
            return Task.FromResult(ContentExists("Locations", locationId));
        }

        public Task<bool> QuestExistsAsync(string questId)
        {
            return Task.FromResult(ContentExists("Quests", questId));
        }

        private async Task<string> ReadContentAsync(string folder, string id)
        {
            var path = GetContentPath(folder, id);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Content file not found: {path}");
            }

            return await File.ReadAllTextAsync(path);
        }

        private string GetContentPath(string folder, string id)
        {
            var folderPath = Path.Combine(_contentRoot, folder, $"{id}.md");
            if (File.Exists(folderPath))
            {
                return folderPath;
            }

            return Path.Combine(_contentRoot, $"{id}.md");
        }

        private bool ContentExists(string folder, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            var folderPath = Path.Combine(_contentRoot, folder, $"{id}.md");
            if (File.Exists(folderPath))
            {
                return true;
            }

            return File.Exists(Path.Combine(_contentRoot, $"{id}.md"));
        }
    }
}
