using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Utils;
using GameShared.Models;

namespace GameBackend.Core.Repositories
{
    public class InventoryRepository : IInventoryRepository
    {
        private readonly Table _table;

        public InventoryRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _table = Table.LoadTable(dynamoDbClient, AppSettings.InventoryTableName);
        }

        /// <inheritdoc/>
        public async Task<List<Inventory>> GetByCharacterIdAsync(string characterId)
        {
            var filter = new ScanFilter();
            filter.AddCondition("characterId", ScanOperator.Equal, characterId);
            var search = _table.Scan(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Select(d => JsonUtils.Deserialize<Inventory>(d.ToJson())!).ToList();
        }

        /// <inheritdoc/>
        public async Task<Inventory?> GetByInventoryIdAsync(string inventoryId)
        {
            var doc = await _table.GetItemAsync(inventoryId);
            return doc == null ? null : JsonUtils.Deserialize<Inventory>(doc.ToJson());
        }

        /// <inheritdoc/>
        public async Task<Inventory?> FindByCharacterAndItemAsync(string characterId, string itemId)
        {
            var filter = new ScanFilter();
            filter.AddCondition("characterId", ScanOperator.Equal, characterId);
            filter.AddCondition("itemId",      ScanOperator.Equal, itemId);
            var search = _table.Scan(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<Inventory>(docs[0].ToJson()) : null;
        }

        /// <inheritdoc/>
        public async Task<List<Inventory>> GetEquippedItemsAsync(string characterId)
        {
            var filter = new ScanFilter();
            filter.AddCondition("characterId", ScanOperator.Equal, characterId);
            filter.AddCondition("equipped",    ScanOperator.Equal, true);
            var search = _table.Scan(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Select(d => JsonUtils.Deserialize<Inventory>(d.ToJson())!).ToList();
        }

        /// <inheritdoc/>
        public async Task<int> CountSlotsAsync(string characterId)
        {
            var filter = new ScanFilter();
            filter.AddCondition("characterId", ScanOperator.Equal, characterId);
            filter.AddCondition("quantity",    ScanOperator.GreaterThan, 0);

            // Chỉ cần đếm — chỉ lấy inventoryId để tiết kiệm đọc từ DynamoDB
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

        /// <inheritdoc/>
        public async Task SaveAsync(Inventory inventory)
        {
            var doc = Document.FromJson(JsonUtils.Serialize(inventory));
            await _table.PutItemAsync(doc);
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(string inventoryId)
        {
            await _table.DeleteItemAsync(inventoryId);
        }
    }
}
