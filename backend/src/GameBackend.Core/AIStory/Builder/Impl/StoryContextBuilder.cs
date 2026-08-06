using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameShared.Models;
using GameBackend.Core.AIStory.DTOs;
using GameBackend.Core.AIStory.Formatters.Interfaces;
using GameBackend.Core.AIStory.Services;
using GameBackend.Core.Repositories.Interfaces;

namespace GameBackend.Core.AIStory.Builder.Impl
{
    public class GamePromptContextBuilder : IGamePromptContextBuilder
    {
        private readonly ICharacterFormatter _characterFormatter;
        private readonly IInventoryFormatter _inventoryFormatter;
        private readonly IRecentTurnsFormatter _recentTurnsFormatter;
        private readonly IContentService _contentService;
        private readonly IDefeatedBossRepository _defeatedBossRepository;

        public GamePromptContextBuilder(
            ICharacterFormatter characterFormatter,
            IInventoryFormatter inventoryFormatter,
            IRecentTurnsFormatter recentTurnsFormatter,
            IContentService contentService,
            IDefeatedBossRepository defeatedBossRepository)
        {
            _characterFormatter = characterFormatter;
            _inventoryFormatter = inventoryFormatter;
            _recentTurnsFormatter = recentTurnsFormatter;
            _contentService = contentService;
            _defeatedBossRepository = defeatedBossRepository;
        }

        public async Task<GamePromptContext> BuildAsync(
            Character character,
            IEnumerable<Item> inventoryItems,
            IEnumerable<StoryAction> recentActions,
            StorySession session,
            string userAction,
            string? systemInjectedEvent = null)
        {
            var world = await _contentService.GetWorldAsync();

            var chapter = await _contentService.GetChapterAsync(session.currentChapterId);

            var location = await _contentService.GetLocationAsync(session.currentLocation);

            var defeatedBosses = await _defeatedBossRepository.GetDefeatedBossesByCharacterIdAsync(character.characterId);
            var defeatedBossesList = defeatedBosses.Select(b => $"- {b.bossName} ({b.bossId})").ToList();
            var defeatedBossesInfo = defeatedBossesList.Count > 0 
                ? string.Join("\n", defeatedBossesList) 
                : "Chưa tiêu diệt Boss nào.";

            return new GamePromptContext
            {
                World = world,

                CharacterInfo = _characterFormatter.Format(character),

                InventoryInfo = _inventoryFormatter.Format(inventoryItems),

                Chapter = chapter,

                Location = location,

                StorySummary = session.storySummary,

                RecentTurns = _recentTurnsFormatter.Format(recentActions),

                UserAction = userAction,

                SystemInjectedEvent = systemInjectedEvent,

                DefeatedBossesInfo = defeatedBossesInfo
            };
        }
    }
}