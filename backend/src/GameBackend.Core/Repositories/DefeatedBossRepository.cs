using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameBackend.Core.Config;
using GameBackend.Core.Utils;

namespace GameBackend.Core.Repositories
{
    public class DefeatedBossRepository : IDefeatedBossRepository
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private Table? _defeatedTable;
        private Table DefeatedTable => _defeatedTable ??= Table.LoadTable(_dynamoDbClient, AppSettings.DefeatedBossesTableName);

        public DefeatedBossRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _dynamoDbClient = dynamoDbClient;
        }

        public async Task SaveDefeatedBossAsync(DefeatedBoss defeatedBoss)
        {
            if (defeatedBoss == null || string.IsNullOrWhiteSpace(defeatedBoss.characterId) || string.IsNullOrWhiteSpace(defeatedBoss.bossId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(defeatedBoss));
            await DefeatedTable.PutItemAsync(doc);
        }

        public async Task<List<DefeatedBoss>> GetDefeatedBossesByCharacterIdAsync(string characterId)
        {
            var results = new List<DefeatedBoss>();
            if (string.IsNullOrWhiteSpace(characterId)) return results;

            var queryFilter = new QueryFilter("characterId", QueryOperator.Equal, characterId);
            var search = DefeatedTable.Query(queryFilter);
            
            do
            {
                var documentList = await search.GetNextSetAsync();
                foreach (var doc in documentList)
                {
                    var model = JsonUtils.Deserialize<DefeatedBoss>(doc.ToJson());
                    if (model != null)
                    {
                        results.Add(model);
                    }
                }
            } while (!search.IsDone);

            return results;
        }

        public async Task<bool> HasDefeatedBossAsync(string characterId, string bossId)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(bossId)) return false;

            var doc = await DefeatedTable.GetItemAsync(characterId, bossId);
            return doc != null;
        }
    }
}
