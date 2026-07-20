using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameShared.DTOs.Story;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
	public class StoryStateUpdater : IStoryStateUpdater
	{
		private readonly IStoryRepository _storyRepository;
		private readonly ICharacterService _characterService;
		private readonly IInventoryRepository _inventoryRepository;
		private readonly IBattleRepository _battleRepository;
		private readonly IBossRepository _bossRepository;
		private readonly ILogger<StoryStateUpdater> _logger;

		public StoryStateUpdater(
			IStoryRepository storyRepository,
			ICharacterService characterService,
			IInventoryRepository inventoryRepository,
			IBattleRepository battleRepository,
			IBossRepository bossRepository,
			ILogger<StoryStateUpdater> logger)
		{
			_storyRepository = storyRepository;
			_characterService = characterService;
			_inventoryRepository = inventoryRepository;
			_battleRepository = battleRepository;
			_bossRepository = bossRepository;
			_logger = logger;
		}

		public async Task ApplyAsync(StorySession session, Character character, StoryAiResponse aiResponse)
		{
			if (session == null) throw new ArgumentNullException(nameof(session));
			if (character == null) throw new ArgumentNullException(nameof(character));
			if (aiResponse == null) throw new ArgumentNullException(nameof(aiResponse));

			ApplySession(session, aiResponse);
			await ApplyCharacterAsync(character, aiResponse);
			await ApplyInventoryAsync(character, aiResponse.InventoryChanges ?? new List<StoryAiInventoryChange>());
			await ApplyBattleAsync(character, aiResponse);

			session.updatedAt = DateTime.UtcNow;
			await _storyRepository.SaveSessionAsync(session);
		}

		private static void ApplySession(StorySession session, StoryAiResponse aiResponse)
		{
			if (!string.IsNullOrWhiteSpace(aiResponse.CurrentNodeId))
			{
				session.currentNodeId = aiResponse.CurrentNodeId;
			}

			if (!string.IsNullOrWhiteSpace(aiResponse.CurrentLocation))
			{
				session.currentLocation = aiResponse.CurrentLocation;
			}

			if (!string.IsNullOrWhiteSpace(aiResponse.CurrentChapterId))
			{
				session.currentChapterId = aiResponse.CurrentChapterId;
			}

			if (!string.IsNullOrWhiteSpace(aiResponse.StorySummary))
			{
				session.storySummary = string.IsNullOrWhiteSpace(session.storySummary)
					? aiResponse.StorySummary
					: $"{session.storySummary} {aiResponse.StorySummary}";
			}
		}

		private async Task ApplyCharacterAsync(Character character, StoryAiResponse aiResponse)
		{
			var delta = aiResponse.CharacterDelta;
			if (delta == null)
			{
				await _characterService.ApplyExperienceAndLevelUp(character, 0);
				return;
			}

			character.hp = Math.Clamp(character.hp + delta.HpDelta, 0, character.maxHp);
			character.mp = Math.Clamp(character.mp + delta.MpDelta, 0, character.maxMp);
			character.gold = Math.Max(0, character.gold + delta.GoldDelta);

			if (!string.IsNullOrWhiteSpace(delta.Status))
			{
				character.status = delta.Status;
			}

			if (!string.IsNullOrWhiteSpace(delta.CurrentLocationId))
			{
				character.currentLocationId = delta.CurrentLocationId;
			}

			await _characterService.ApplyExperienceAndLevelUp(character, Math.Max(0, delta.ExpDelta));
		}

		private async Task ApplyInventoryAsync(Character character, List<StoryAiInventoryChange> inventoryChanges)
		{
			foreach (var change in inventoryChanges)
			{
				if (change == null || string.IsNullOrWhiteSpace(change.ItemId) || change.QuantityDelta == 0)
				{
					continue;
				}

				var existing = await _inventoryRepository.FindByCharacterAndItemAsync(character.characterId, change.ItemId);
				if (existing == null)
				{
					if (change.QuantityDelta < 0)
					{
						continue;
					}

					var newItem = new Inventory
					{
						inventoryId = Guid.NewGuid().ToString("N"),
						characterId = character.characterId,
						itemId = change.ItemId,
						quantity = change.QuantityDelta,
						equipped = change.Equipped,
						slotIndex = change.SlotIndex ?? 0,
						locked = change.Locked,
						acquiredAt = DateTime.UtcNow
					};

					await _inventoryRepository.SaveAsync(newItem);
					continue;
				}

				existing.quantity = Math.Max(0, existing.quantity + change.QuantityDelta);
				existing.equipped = change.Equipped;
				existing.slotIndex = change.SlotIndex ?? existing.slotIndex;
				existing.locked = change.Locked;
				await _inventoryRepository.SaveAsync(existing);
			}
		}

		private async Task ApplyBattleAsync(Character character, StoryAiResponse aiResponse)
		{
			if (!aiResponse.TriggerBattle || string.IsNullOrWhiteSpace(aiResponse.BossId))
			{
				return;
			}

			var boss = await _bossRepository.GetByIdAsync(aiResponse.BossId);
			if (boss == null)
			{
				_logger.LogWarning("Skip encounter creation because boss does not exist: {BossId}", aiResponse.BossId);
				return;
			}

			var encounter = new BossEncounter
			{
				encounterId = Guid.NewGuid().ToString("N"),
				characterId = character.characterId,
				bossId = boss.bossId,
				bossLevel = aiResponse.BossLevel ?? (boss.level > 0 ? boss.level : 1),
				playerHpBefore = character.hp,
				playerHpAfter = character.hp,
				bossHpBefore = Math.Max(1, boss.baseHp),
				bossHpAfter = Math.Max(1, boss.baseHp),
				status = "Active",
				encounterTime = DateTime.UtcNow
			};

			await _battleRepository.SaveEncounterAsync(encounter);
		}
	}
}
 