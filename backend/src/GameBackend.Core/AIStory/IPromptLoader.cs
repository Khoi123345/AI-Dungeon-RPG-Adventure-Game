namespace GameBackend.Core.AIStory
{
    /// <summary>
    /// Interface trừu tượng cho việc load nội dung prompt markdown.
    /// Có 2 implementation:
    ///   - FileSystemPromptLoader: đọc file local (dev/test, không cần S3)
    ///   - S3PromptLoader: đọc từ AWS S3, cache trong memory suốt vòng đời Lambda
    /// </summary>
    public interface IPromptLoader
    {
        /// <summary>
        /// Đọc system prompt từ nguồn cấu hình.
        /// </summary>
        Task<string> GetSystemPromptAsync();

        /// <summary>
        /// Đọc user story prompt template từ nguồn cấu hình.
        /// </summary>
        Task<string> GetStoryPromptAsync();
    }
}
