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
    public class StoryRepository : IStoryRepository
    {
        private readonly Table _table;

        public StoryRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _table = Table.LoadTable(dynamoDbClient, AppSettings.GameTableName);
        }

        public async Task<StorySession?> GetSessionByIdAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return null;

            var doc = await _table.GetItemAsync($"SESSION#{sessionId}", "METADATA");
            return doc != null ? JsonUtils.Deserialize<StorySession>(doc.ToJson()) : null;
        }

        public async Task<StorySession?> GetSessionByCharacterIdAsync(string characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return null;

            var search = _table.Query(new QueryOperationConfig
            {
                IndexName = "GSI1",
                Filter = new QueryFilter("GSI1PK", QueryOperator.Equal, $"CHAR#{characterId}")
            });
            var docs = await search.GetNextSetAsync();
            var sessions = docs
                .Where(d => d.ContainsKey("SK") && d["SK"].AsString() == "METADATA")
                .Select(d => JsonUtils.Deserialize<StorySession>(d.ToJson())!)
                .Where(s => s != null && s.status == "Active")
                .OrderByDescending(s => s.updatedAt)
                .ToList();

            return sessions.FirstOrDefault();
        }

        public async Task SaveSessionAsync(StorySession session)
        {
            if (session == null || string.IsNullOrWhiteSpace(session.sessionId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(session));
            doc["PK"] = $"SESSION#{session.sessionId}";
            doc["SK"] = "METADATA";
            doc["GSI1PK"] = $"CHAR#{session.characterId}";
            doc["GSI1SK"] = $"SESSION#{session.sessionId}";

            await _table.PutItemAsync(doc);
        }

        public async Task SaveActionAsync(StoryAction action)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.actionId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(action));
            doc["PK"] = $"SESSION#{action.sessionId}";
            doc["SK"] = $"ACTION#{action.actionId}";
            doc["GSI1PK"] = $"SESSION#{action.sessionId}";
            doc["GSI1SK"] = $"ACTION#{action.turnNumber:D6}";

            await _table.PutItemAsync(doc);
        }

        public async Task<List<StoryAction>> GetActionsBySessionIdAsync(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return new List<StoryAction>();

            var filter = new QueryFilter("PK", QueryOperator.Equal, $"SESSION#{sessionId}");
            filter.AddCondition("SK", QueryOperator.BeginsWith, "ACTION#");

            var search = _table.Query(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Select(d => JsonUtils.Deserialize<StoryAction>(d.ToJson())!).ToList();
        }
    }
}
