using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System;
using System.Threading.Tasks;
using GameBackend.Core.Config;
using GameBackend.Core.Utils;

namespace GameBackend.Core.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private Table? _table;
        private Table Table => _table ??= Table.LoadTable(_dynamoDbClient, AppSettings.UsersTableName);

        public UserRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _dynamoDbClient = dynamoDbClient;
        }

        public async Task<User?> GetByIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var doc = await Table.GetItemAsync(userId);
            return doc != null ? JsonUtils.Deserialize<User>(doc.ToJson()) : null;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;

            var filter = new ScanFilter();
            filter.AddCondition("username", ScanOperator.Equal, username);
            var search = Table.Scan(filter);
            var docs = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<User>(docs[0].ToJson()) : null;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return null;

            var filter = new ScanFilter();
            filter.AddCondition("email", ScanOperator.Equal, email.ToLowerInvariant());
            var search = Table.Scan(filter);
            var docs   = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<User>(docs[0].ToJson()) : null;
        }

        public async Task<User?> GetByCognitoSubAsync(string cognitoSub)
        {
            if (string.IsNullOrWhiteSpace(cognitoSub)) return null;

            var filter = new ScanFilter();
            filter.AddCondition("cognitoSub", ScanOperator.Equal, cognitoSub);
            var search = Table.Scan(filter);
            var docs   = await search.GetNextSetAsync();
            return docs.Count > 0 ? JsonUtils.Deserialize<User>(docs[0].ToJson()) : null;
        }

        public async Task SaveAsync(User user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.userId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(user));
            await Table.PutItemAsync(doc);
        }
    }
}
