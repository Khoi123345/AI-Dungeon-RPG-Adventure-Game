namespace GameBackend.Core.AIStory
{
    /// <summary>
    /// Đọc prompt markdown từ filesystem local.
    /// Dùng cho môi trường dev/test khi không có S3.
    /// Khi PROMPT_SOURCE env var = "file" (hoặc không set).
    /// </summary>
    public class FileSystemPromptLoader : IPromptLoader
    {
        private readonly string _promptDir;

        /// <param name="templatePath">
        /// Đường dẫn tới thư mục chứa Content/Prompt/ hoặc trực tiếp tới thư mục Prompt/.
        /// </param>
        public FileSystemPromptLoader(string templatePath)
        {
            // Hỗ trợ 2 cấu trúc thư mục:
            //   1. <root>/Content/Prompt/system_prompt.md  (cấu trúc mới)
            //   2. <root>/system_prompt.md                 (legacy/trực tiếp)
            var contentPromptDir = Path.Combine(templatePath, "Content", "Prompt");
            _promptDir = Directory.Exists(contentPromptDir) ? contentPromptDir : templatePath;
        }

        public Task<string> GetSystemPromptAsync() =>
            ReadFileAsync("system_prompt.md");

        public Task<string> GetStoryPromptAsync() =>
            ReadFileAsync("story_prompt.md");

        private Task<string> ReadFileAsync(string fileName)
        {
            var filePath = Path.Combine(_promptDir, fileName);
            if (File.Exists(filePath))
                return Task.FromResult(File.ReadAllText(filePath));

            return Task.FromResult(string.Empty); // PromptBuilder có fallback mặc định
        }
    }
}
