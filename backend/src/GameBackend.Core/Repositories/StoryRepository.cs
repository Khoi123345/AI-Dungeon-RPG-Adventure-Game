using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System.Text.Json;
using GameBackend.Core.Utils;

namespace GameBackend.Core.Repositories
{
    public class StoryRepository : IStoryRepository
    {
        private readonly Table _sessionTable;
        private readonly Table _actionTable;

        public StoryRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _sessionTable = Table.LoadTable(dynamoDbClient, AppSettings.StorySessionsTableName);
            _actionTable = Table.LoadTable(dynamoDbClient, AppSettings.StoryActionsTableName);
        }

        public async Task<StorySession?> GetSessionByIdAsync(string sessionId)
        {
            var doc = await _sessionTable.GetItemAsync(sessionId);
            return doc != null ? JsonUtils.Deserialize<StorySession>(doc.ToJson()) : null;
        }

        public async Task<StorySession?> GetSessionByCharacterIdAsync(string characterId)
        {
            var filter = new ScanFilter();
            filter.AddCondition("characterId", ScanOperator.Equal, characterId);
            filter.AddCondition("status", ScanOperator.Equal, "Active");
            var search = _sessionTable.Scan(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<StorySession>(docs[0].ToJson()) : null;
        }

        public async Task SaveSessionAsync(StorySession session)
        {
            var doc = Document.FromJson(JsonUtils.Serialize(session));
            await _sessionTable.PutItemAsync(doc);
        }

        public async Task SaveActionAsync(StoryAction action)
        {
            var doc = Document.FromJson(JsonUtils.Serialize(action));
            await _actionTable.PutItemAsync(doc);
        }

        public async Task<List<StoryAction>> GetActionsBySessionIdAsync(string sessionId)
        {
            var filter = new ScanFilter();
            filter.AddCondition("sessionId", ScanOperator.Equal, sessionId);
            var search = _actionTable.Scan(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Select(d => JsonUtils.Deserialize<StoryAction>(d.ToJson())!).ToList();
        }
    }
}
