using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System.Text.Json;
using GameBackend.Core.Utils;

namespace GameBackend.Core.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly Table _table;

        public UserRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _table = Table.LoadTable(dynamoDbClient, AppSettings.UsersTableName);
        }

        public async Task<User?> GetByIdAsync(string userId)
        {
            var doc = await _table.GetItemAsync(userId);
            return doc != null ? JsonUtils.Deserialize<User>(doc.ToJson()) : null;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            var filter = new ScanFilter();
            filter.AddCondition("username", ScanOperator.Equal, username);
            var search = _table.Scan(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<User>(docs[0].ToJson()) : null;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            var filter = new ScanFilter();
            filter.AddCondition("email", ScanOperator.Equal, email.ToLowerInvariant());
            var search = _table.Scan(filter);
            var docs   = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<User>(docs[0].ToJson()) : null;
        }

        public async Task<User?> GetByCognitoSubAsync(string cognitoSub)
        {
            var filter = new ScanFilter();
            filter.AddCondition("cognitoSub", ScanOperator.Equal, cognitoSub);
            var search = _table.Scan(filter);
            var docs   = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<User>(docs[0].ToJson()) : null;
        }

        public async Task SaveAsync(User user)
        {
            var doc = Document.FromJson(JsonUtils.Serialize(user));
            await _table.PutItemAsync(doc);
        }
    }

    internal static class AppSettings
    {
        public static string UsersTableName => Environment.GetEnvironmentVariable("USERS_TABLE") ?? "GameUsers";
        public static string CharactersTableName => Environment.GetEnvironmentVariable("CHARACTERS_TABLE") ?? "GameCharacters";
        public static string BossesTableName => Environment.GetEnvironmentVariable("BOSSES_TABLE") ?? "GameBosses";
        public static string EncountersTableName => Environment.GetEnvironmentVariable("ENCOUNTERS_TABLE") ?? "GameBossEncounters";
        public static string BattlesTableName => Environment.GetEnvironmentVariable("BATTLES_TABLE") ?? "GameBattles";
        public static string LootDropsTableName => Environment.GetEnvironmentVariable("LOOT_DROPS_TABLE") ?? "GameLootDrops";
        public static string StorySessionsTableName => Environment.GetEnvironmentVariable("STORY_SESSIONS_TABLE") ?? "GameStorySessions";
        public static string StoryActionsTableName => Environment.GetEnvironmentVariable("STORY_ACTIONS_TABLE") ?? "GameStoryActions";
        public static string InventoryTableName => Environment.GetEnvironmentVariable("INVENTORY_TABLE") ?? "GameInventory";
    }
}
