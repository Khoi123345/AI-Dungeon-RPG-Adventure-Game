using System.IO;
using System.Threading.Tasks;

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
            var path = Path.Combine(_contentRoot, "World", "world.md");
            return await File.ReadAllTextAsync(path);
        }

        public async Task<string> GetChapterAsync(string chapterId)
        {
            var path = Path.Combine(_contentRoot, "Chapters", $"{chapterId}.md");
            return await File.ReadAllTextAsync(path);
        }

        public async Task<string> GetLocationAsync(string locationId)
        {
            var path = Path.Combine(_contentRoot, "Locations", $"{locationId}.md");
            return await File.ReadAllTextAsync(path);
        }
    }
}