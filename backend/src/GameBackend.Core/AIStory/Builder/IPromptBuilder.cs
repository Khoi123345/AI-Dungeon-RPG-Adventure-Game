using GameBackend.Core.AIStory.DTOs;

namespace GameBackend.Core.AIStory
{
    public interface IPromptBuilder
    {
        /// <summary>
        /// Build prompt đồng bộ (backward compat).
        /// </summary>
        (string SystemPrompt, string UserPrompt) Build(GamePromptContext context);

        /// <summary>
        /// Build prompt bất đồng bộ — dùng khi loader là S3 (async I/O).
        /// </summary>
        Task<(string SystemPrompt, string UserPrompt)> BuildAsync(GamePromptContext context);
    }
}