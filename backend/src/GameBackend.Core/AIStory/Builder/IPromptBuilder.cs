using GameBackend.Core.AIStory.DTOs;

namespace GameBackend.Core.AIStory
{
    public interface IPromptBuilder
    {
        string Build(PromptContext context);
    }
}