using GameBackend.Core.Repositories.Interfaces;

namespace GameBackend.Core.Repositories
{
    public sealed class InMemoryItemRepository : IItemRepository
    {
        private static readonly HashSet<string> ValidItemIds = new(StringComparer.OrdinalIgnoreCase)
        {
            "ancient_key",
            "rusty_sword",
            "healing_potion",
            "mana_potion",
            "torch",
            "gold_coin"
        };

        public Task<bool> ExistsAsync(string itemId)
        {
            var exists = !string.IsNullOrWhiteSpace(itemId) && ValidItemIds.Contains(itemId);
            return Task.FromResult(exists);
        }
    }
}
