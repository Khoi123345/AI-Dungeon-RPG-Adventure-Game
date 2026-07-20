using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameBackend.Core.Utils;

namespace GameBackend.Core.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly Table _table;

        public UserRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _table = Table.LoadTable(dynamoDbClient, AppSettings.GameTableName);
        }

        public async Task<User?> GetByIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var doc = await _table.GetItemAsync($"USER#{userId}", "METADATA");
            return doc != null ? JsonUtils.Deserialize<User>(doc.ToJson()) : null;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            var search = _table.Query(new QueryOperationConfig
            {
                IndexName = "GSI1",
                Filter = new QueryFilter("GSI1PK", QueryOperator.Equal, $"USERNAME#{username}")
            });
            var docs = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<User>(docs[0].ToJson()) : null;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            var search = _table.Query(new QueryOperationConfig
            {
                IndexName = "GSI1",
                Filter = new QueryFilter("GSI1PK", QueryOperator.Equal, $"EMAIL#{email.ToLowerInvariant()}")
            });
            var docs = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<User>(docs[0].ToJson()) : null;
        }

        public async Task<User?> GetByCognitoSubAsync(string cognitoSub)
        {
            if (string.IsNullOrWhiteSpace(cognitoSub)) return null;

            var search = _table.Query(new QueryOperationConfig
            {
                IndexName = "GSI1",
                Filter = new QueryFilter("GSI1PK", QueryOperator.Equal, $"COGNITO#{cognitoSub}")
            });
            var docs = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<User>(docs[0].ToJson()) : null;
        }

        public async Task SaveAsync(User user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.userId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(user));
            doc["PK"] = $"USER#{user.userId}";
            doc["SK"] = "METADATA";
            doc["GSI1PK"] = $"USERNAME#{user.username}";
            doc["GSI1SK"] = $"USER#{user.userId}";

            await _table.PutItemAsync(doc);

            // GSI index records cho Email và CognitoSub (nếu có)
            if (!string.IsNullOrWhiteSpace(user.email))
            {
                var emailDoc = Document.FromJson(JsonUtils.Serialize(user));
                emailDoc["PK"] = $"EMAIL#{user.email.ToLowerInvariant()}";
                emailDoc["SK"] = "METADATA";
                emailDoc["GSI1PK"] = $"EMAIL#{user.email.ToLowerInvariant()}";
                emailDoc["GSI1SK"] = $"USER#{user.userId}";
                await _table.PutItemAsync(emailDoc);
            }

            if (!string.IsNullOrWhiteSpace(user.cognitoSub))
            {
                var cognitoDoc = Document.FromJson(JsonUtils.Serialize(user));
                cognitoDoc["PK"] = $"COGNITO#{user.cognitoSub}";
                cognitoDoc["SK"] = "METADATA";
                cognitoDoc["GSI1PK"] = $"COGNITO#{user.cognitoSub}";
                cognitoDoc["GSI1SK"] = $"USER#{user.userId}";
                await _table.PutItemAsync(cognitoDoc);
            }
        }
    }

    internal static class AppSettings
    {
        public static string GameTableName => Environment.GetEnvironmentVariable("GAME_TABLE") ?? Environment.GetEnvironmentVariable("USERS_TABLE") ?? "GameTable";
        public static string UsersTableName => GameTableName;
        public static string CharactersTableName => GameTableName;
        public static string BossesTableName => GameTableName;
        public static string EncountersTableName => GameTableName;
        public static string BattlesTableName => GameTableName;
        public static string LootDropsTableName => GameTableName;
        public static string StorySessionsTableName => GameTableName;
        public static string StoryActionsTableName => GameTableName;
        public static string InventoryTableName => GameTableName;
    }
}
