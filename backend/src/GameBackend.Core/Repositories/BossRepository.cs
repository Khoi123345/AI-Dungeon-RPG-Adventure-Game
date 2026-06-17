using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System.Text.Json;

namespace GameBackend.Core.Repositories
{
    public class BossRepository : IBossRepository
    {
        private readonly Table _table;

        public BossRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _table = Table.LoadTable(dynamoDbClient, AppSettings.BossesTableName);
        }

        public async Task<Boss?> GetByIdAsync(string bossId)
        {
            var doc = await _table.GetItemAsync(bossId);
            return doc != null ? JsonSerializer.Deserialize<Boss>(doc.ToJson()) : null;
        }

        public async Task SaveAsync(Boss boss)
        {
            var doc = Document.FromJson(JsonSerializer.Serialize(boss));
            await _table.PutItemAsync(doc);
        }
    }
}
