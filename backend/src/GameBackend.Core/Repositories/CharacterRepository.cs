using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameBackend.Core.Utils;

namespace GameBackend.Core.Repositories
{
    public class CharacterRepository : ICharacterRepository
    {
        private readonly Table _table;

        public CharacterRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _table = Table.LoadTable(dynamoDbClient, AppSettings.GameTableName);
        }

        public async Task<Character?> GetByIdAsync(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return null;

            var doc = await _table.GetItemAsync($"CHAR#{characterId}", "METADATA");
            return doc != null ? JsonUtils.Deserialize<Character>(doc.ToJson()) : null;
        }

        public async Task<List<Character>> GetByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return new List<Character>();

            var search = _table.Query(new QueryOperationConfig
            {
                IndexName = "GSI1",
                Filter = new QueryFilter("GSI1PK", QueryOperator.Equal, $"USER#{userId}")
            });
            var docs = await search.GetNextSetAsync();
            return docs.Select(d => JsonUtils.Deserialize<Character>(d.ToJson())!).ToList();
        }

        public async Task SaveAsync(Character character)
        {
            if (character == null || string.IsNullOrWhiteSpace(character.characterId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(character));
            doc["PK"] = $"CHAR#{character.characterId}";
            doc["SK"] = "METADATA";
            doc["GSI1PK"] = $"USER#{character.userId}";
            doc["GSI1SK"] = $"CHAR#{character.characterId}";

            await _table.PutItemAsync(doc);
        }
    }
}
