using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System.Threading.Tasks;
using GameBackend.Core.Config;
using GameBackend.Core.Utils;

namespace GameBackend.Core.Repositories
{
    public class BossRepository : IBossRepository
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private Table? _table;
        private Table Table => _table ??= Table.LoadTable(_dynamoDbClient, AppSettings.BossesTableName);

        public BossRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _dynamoDbClient = dynamoDbClient;
        }

        public async Task<Boss?> GetByIdAsync(string bossId)
        {
            if (string.IsNullOrWhiteSpace(bossId)) return null;

            try
            {
                var doc = await Table.GetItemAsync(bossId);
                if (doc != null)
                {
                    var boss = JsonUtils.Deserialize<Boss>(doc.ToJson());
                    if (boss != null) return boss;
                }
            }
            catch { }

            return GameShared.Config.GameConstants.BossCatalog
                .FirstOrDefault(b => string.Equals(b.bossId, bossId, System.StringComparison.OrdinalIgnoreCase));
        }

        public async Task SaveAsync(Boss boss)
        {
            if (boss == null || string.IsNullOrWhiteSpace(boss.bossId)) return;

            try
            {
                var doc = Document.FromJson(JsonUtils.Serialize(boss));
                await Table.PutItemAsync(doc);
            }
            catch { }
        }
    }
}
