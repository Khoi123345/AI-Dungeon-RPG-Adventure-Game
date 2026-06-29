using GameBackend.Core.Repositories.Interfaces;

namespace GameBackend.Core.Repositories
{
    public sealed class InMemoryLootRepository : ILootRepository
    {
        private static readonly Dictionary<string, HashSet<string>> AllowedDropsByLocation = new(StringComparer.OrdinalIgnoreCase)
        {
            ["ancient_cave_depths"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ancient_key",
                "torch"
            },
            ["dragon_cave"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "gold_coin",
                "healing_potion"
            },
            ["Ancient Ruins"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "rusty_sword"
            }
        };

        public Task<bool> CanDropItemAtLocationAsync(string itemId, string locationId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || string.IsNullOrWhiteSpace(locationId))
            {
                return Task.FromResult(false);
            }

            if (!AllowedDropsByLocation.TryGetValue(locationId, out var allowedItems))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(allowedItems.Contains(itemId));
        }
    }
}
