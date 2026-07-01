using System.Collections.Generic;
using System.Threading.Tasks;
using GameShared.Models;
using GameBackend.Core.AIStory.DTOs;

namespace GameBackend.Core.AIStory.Builder
{
    public interface IStoryContextBuilder
    {
        Task<PromptContext> BuildAsync(
            Character character,
            IEnumerable<Item> inventoryItems,
            IEnumerable<StoryAction> recentActions,
            StorySession session,
            string userAction);
    }
}