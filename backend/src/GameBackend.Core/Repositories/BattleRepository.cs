using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System.Text.Json;
using GameBackend.Core.Utils;

namespace GameBackend.Core.Repositories
{
    public class BattleRepository : IBattleRepository
    {
        private readonly Table _encounterTable;
        private readonly Table _battleTable;
        private readonly Table _lootTable;

        public BattleRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _encounterTable = Table.LoadTable(dynamoDbClient, AppSettings.EncountersTableName);
            _battleTable = Table.LoadTable(dynamoDbClient, AppSettings.BattlesTableName);
            _lootTable = Table.LoadTable(dynamoDbClient, AppSettings.LootDropsTableName);
        }

        public async Task<BossEncounter?> GetEncounterByIdAsync(string encounterId)
        {
            var doc = await _encounterTable.GetItemAsync(encounterId);
            return doc != null ? JsonUtils.Deserialize<BossEncounter>(doc.ToJson()) : null;
        }

        public async Task SaveEncounterAsync(BossEncounter encounter)
        {
            var doc = Document.FromJson(JsonUtils.Serialize(encounter));
            await _encounterTable.PutItemAsync(doc);
        }

        public async Task<Battle?> GetBattleByIdAsync(string battleId)
        {
            var doc = await _battleTable.GetItemAsync(battleId);
            return doc != null ? JsonUtils.Deserialize<Battle>(doc.ToJson()) : null;
        }

        public async Task SaveBattleAsync(Battle battle)
        {
            var doc = Document.FromJson(JsonUtils.Serialize(battle));
            await _battleTable.PutItemAsync(doc);
        }

        public async Task SaveLootDropAsync(LootDrop lootDrop)
        {
            var doc = Document.FromJson(JsonUtils.Serialize(lootDrop));
            await _lootTable.PutItemAsync(doc);
        }
    }
}
