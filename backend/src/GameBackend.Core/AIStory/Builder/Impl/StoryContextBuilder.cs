using System.Collections.Generic;
using System.Threading.Tasks;
using GameShared.Models;
using GameBackend.Core.AIStory.DTOs;
using GameBackend.Core.AIStory.Formatters.Interfaces;
using GameBackend.Core.AIStory.Services;

namespace GameBackend.Core.AIStory.Builder.Impl
{
    public class GamePromptContextBuilder : IGamePromptContextBuilder
    {
        private readonly ICharacterFormatter _characterFormatter;
        private readonly IInventoryFormatter _inventoryFormatter;
        private readonly IRecentTurnsFormatter _recentTurnsFormatter;
        private readonly IContentService _contentService;

        public GamePromptContextBuilder(
            ICharacterFormatter characterFormatter,
            IInventoryFormatter inventoryFormatter,
            IRecentTurnsFormatter recentTurnsFormatter,
            IContentService contentService)
        {
            _characterFormatter = characterFormatter;
            _inventoryFormatter = inventoryFormatter;
            _recentTurnsFormatter = recentTurnsFormatter;
            _contentService = contentService;
        }

        public async Task<GamePromptContext> BuildAsync(
            Character character,
            IEnumerable<Item> inventoryItems,
            IEnumerable<StoryAction> recentActions,
            StorySession session,
            string userAction)
        {
            var world = await _contentService.GetWorldAsync();

            var chapter = await _contentService.GetChapterAsync(session.currentChapterId);

            var location = await _contentService.GetLocationAsync(session.currentLocation);

            return new GamePromptContext
            {
                World = world,

                CharacterInfo = _characterFormatter.Format(character),

                InventoryInfo = _inventoryFormatter.Format(inventoryItems),

                Chapter = chapter,

                Location = location,

                StorySummary = session.storySummary,

                RecentTurns = _recentTurnsFormatter.Format(recentActions),

                UserAction = userAction
            };
        }
    }
}