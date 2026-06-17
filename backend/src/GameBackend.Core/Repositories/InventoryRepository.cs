using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System.Text.Json;

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
            var filter = new ScanFilter();
            filter.AddCondition("characterId", ScanOperator.Equal, characterId);
            var search = _table.Scan(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Select(d => JsonSerializer.Deserialize<Inventory>(d.ToJson())!).ToList();
        }

        public async Task<Inventory?> FindByCharacterAndItemAsync(string characterId, string itemId)
        {
            var filter = new ScanFilter();
            filter.AddCondition("characterId", ScanOperator.Equal, characterId);
            filter.AddCondition("itemId", ScanOperator.Equal, itemId);
            var search = _table.Scan(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonSerializer.Deserialize<Inventory>(docs[0].ToJson()) : null;
        }

        public async Task SaveAsync(Inventory inventory)
        {
            var doc = Document.FromJson(JsonSerializer.Serialize(inventory));
            await _table.PutItemAsync(doc);
        }
    }
}
