using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Config;
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
            _table = Table.LoadTable(dynamoDbClient, AppSettings.InventoryTableName);
        }

        public async Task<List<Inventory>> GetByCharacterIdAsync(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return new List<Inventory>();

            var filter = new ScanFilter();
            filter.AddCondition("characterId", ScanOperator.Equal, characterId);
            var search = _table.Scan(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Select(d => JsonUtils.Deserialize<Inventory>(d.ToJson())!).ToList();
        }

        public async Task<Inventory?> GetByInventoryIdAsync(string inventoryId)
        {
            if (string.IsNullOrWhiteSpace(inventoryId)) return null;

            var doc = await _table.GetItemAsync(inventoryId);
            return doc == null ? null : JsonUtils.Deserialize<Inventory>(doc.ToJson());
        }

        public async Task<Inventory?> FindByCharacterAndItemAsync(string characterId, string itemId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(itemId)) return null;

            var filter = new ScanFilter();
            filter.AddCondition("characterId", ScanOperator.Equal, characterId);
            filter.AddCondition("itemId",      ScanOperator.Equal, itemId);
            var search = _table.Scan(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<Inventory>(docs[0].ToJson()) : null;
        }

        public async Task<List<Inventory>> GetEquippedItemsAsync(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return new List<Inventory>();

            var filter = new ScanFilter();
            filter.AddCondition("characterId", ScanOperator.Equal, characterId);
            filter.AddCondition("equipped",    ScanOperator.Equal, true);
            var search = _table.Scan(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Select(d => JsonUtils.Deserialize<Inventory>(d.ToJson())!).ToList();
        }

        public async Task<int> CountSlotsAsync(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return 0;

            var filter = new ScanFilter();
            filter.AddCondition("characterId", ScanOperator.Equal, characterId);
            filter.AddCondition("quantity",    ScanOperator.GreaterThan, 0);

            var config = new ScanOperationConfig
            {
                Filter = filter,
                AttributesToGet = new List<string> { "inventoryId" },
                Select = SelectValues.SpecificAttributes
            };

            var search = _table.Scan(config);
            int count = 0;
            while (!search.IsDone)
            {
                var page = await search.GetNextSetAsync();
                count += page.Count;
            }
            return count;
        }

        public async Task SaveAsync(Inventory inventory)
        {
            if (inventory == null || string.IsNullOrWhiteSpace(inventory.inventoryId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(inventory));
            await _table.PutItemAsync(doc);
        }

        public async Task DeleteAsync(string inventoryId)
        {
            if (string.IsNullOrWhiteSpace(inventoryId)) return;

            await _table.DeleteItemAsync(inventoryId);
        }
    }
}
