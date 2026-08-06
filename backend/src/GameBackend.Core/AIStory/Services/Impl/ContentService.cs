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
            var resolvedId = NormalizeChapterId(chapterId);
            return await ReadContentAsync("Chapters", resolvedId);
        }

        public async Task<string> GetLocationAsync(string locationId)
        {
            var resolvedId = NormalizeLocationId(locationId);
            return await ReadContentAsync("Locations", resolvedId);
        }

        private static string NormalizeChapterId(string chapterId)
        {
            if (string.IsNullOrWhiteSpace(chapterId) || chapterId.Equals("introduction", System.StringComparison.OrdinalIgnoreCase) || chapterId.Equals("prologue", System.StringComparison.OrdinalIgnoreCase) || chapterId.Equals("1"))
            {
                return "chapter_1";
            }
            if (chapterId.Equals("2")) return "chapter_2";
            if (chapterId.Equals("3")) return "chapter_3";
            return chapterId;
        }

        private static string NormalizeLocationId(string locationId)
        {
            if (string.IsNullOrWhiteSpace(locationId) || locationId.Equals("prologue", System.StringComparison.OrdinalIgnoreCase) || locationId.Equals("start", System.StringComparison.OrdinalIgnoreCase))
            {
                return "ancient_cave";
            }
            return locationId;
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
            if (string.IsNullOrWhiteSpace(id))
            {
                return $"No description available for {folder}.";
            }

            var normalizedId = id.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");

            var path = GetContentPath(folder, normalizedId);
            if (!File.Exists(path))
            {
                path = GetContentPath(folder, id);
            }

            if (!File.Exists(path))
            {
                return $"Description for {folder} '{id}' is not available.";
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

            var cleanId = id.StartsWith("boss_") ? id[5..] : id;
            var cleanFolderPath = Path.Combine(_contentRoot, folder, $"{cleanId}.md");
            if (File.Exists(cleanFolderPath))
            {
                return cleanFolderPath;
            }

            return Path.Combine(_contentRoot, $"{id}.md");
        }

        private bool ContentExists(string folder, string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            var normalizedId = id.Trim().ToLowerInvariant().Replace(" ", "_").Replace("-", "_");

            var folderPath = Path.Combine(_contentRoot, folder, $"{normalizedId}.md");
            if (File.Exists(folderPath))
            {
                return true;
            }

            if (File.Exists(Path.Combine(_contentRoot, $"{normalizedId}.md")))
            {
                return true;
            }

            folderPath = Path.Combine(_contentRoot, folder, $"{id}.md");
            if (File.Exists(folderPath))
            {
                return true;
            }

            var cleanId = id.StartsWith("boss_") ? id[5..] : id;
            var cleanFolderPath = Path.Combine(_contentRoot, folder, $"{cleanId}.md");
            if (File.Exists(cleanFolderPath))
            {
                return true;
            }

            return File.Exists(Path.Combine(_contentRoot, $"{id}.md"));
        }
    }
}
