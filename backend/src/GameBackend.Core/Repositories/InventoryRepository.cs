using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Utils;
using GameShared.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GameBackend.Core.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly Table _table;

        public InventoryRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _table = Table.LoadTable(dynamoDbClient, AppSettings.GameTableName);
        }

        public async Task<List<Inventory>> GetByCharacterIdAsync(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return new List<Inventory>();

            var filter = new QueryFilter("PK", QueryOperator.Equal, $"CHAR#{characterId}");
            filter.AddCondition("SK", QueryOperator.BeginsWith, "INVENTORY#");

            var search = _table.Query(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Select(d => JsonUtils.Deserialize<Inventory>(d.ToJson())!).ToList();
        }

        public async Task<Inventory?> GetByInventoryIdAsync(string inventoryId)
        {
            if (string.IsNullOrWhiteSpace(inventoryId)) return null;

            var search = _table.Query(new QueryOperationConfig
            {
                IndexName = "GSI1",
                Filter = new QueryFilter("GSI1PK", QueryOperator.Equal, $"INVENTORY#{inventoryId}")
            });
            var docs = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<Inventory>(docs[0].ToJson()) : null;
        }

        public async Task<Inventory?> FindByCharacterAndItemAsync(string characterId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(itemId)) return null;

            var items = await GetByCharacterIdAsync(characterId);
            return items.FirstOrDefault(x => x.itemId == itemId);
        }

        public async Task<List<Inventory>> GetEquippedItemsAsync(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return new List<Inventory>();

            var items = await GetByCharacterIdAsync(characterId);
            return items.Where(x => x.equipped).ToList();
        }

        public async Task<int> CountSlotsAsync(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return 0;

            var items = await GetByCharacterIdAsync(characterId);
            return items.Count(x => x.quantity > 0);
        }

        public async Task SaveAsync(Inventory inventory)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(inventory.inventoryId) || string.IsNullOrWhiteSpace(inventory.characterId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(inventory));
            doc["PK"] = $"CHAR#{inventory.characterId}";
            doc["SK"] = $"INVENTORY#{inventory.inventoryId}";
            doc["GSI1PK"] = $"INVENTORY#{inventory.inventoryId}";
            doc["GSI1SK"] = "METADATA";

            await _table.PutItemAsync(doc);
        }

        public async Task DeleteAsync(string inventoryId)
        {
            if (string.IsNullOrWhiteSpace(inventoryId)) return;

            var existing = await GetByInventoryIdAsync(inventoryId);
            if (existing != null)
            {
                await _table.DeleteItemAsync($"CHAR#{existing.characterId}", $"INVENTORY#{existing.inventoryId}");
            }
        }
    }
}
