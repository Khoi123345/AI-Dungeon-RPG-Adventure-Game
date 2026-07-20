using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System.Threading.Tasks;
using GameBackend.Core.Utils;

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
            if (string.IsNullOrWhiteSpace(bossId)) return null;

            var doc = await _table.GetItemAsync(bossId);
            return doc != null ? JsonUtils.Deserialize<Boss>(doc.ToJson()) : null;
        }

        public async Task SaveAsync(Boss boss)
        {
            if (boss == null || string.IsNullOrWhiteSpace(boss.bossId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(boss));
            await _table.PutItemAsync(doc);
        }
    }
}
