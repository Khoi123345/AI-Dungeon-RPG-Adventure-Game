using GameBackend.Core.AIStory.DTOs;

namespace GameBackend.Core.AIStory
{
    public interface IPromptBuilder
    {
        (string SystemPrompt, string UserPrompt) Build(GamePromptContext context);
    }
}