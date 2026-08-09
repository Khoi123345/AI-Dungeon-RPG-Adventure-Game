using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System.Threading.Tasks;
using GameBackend.Core.Config;
using GameBackend.Core.Utils;

namespace GameBackend.Core.Repositories
{
    public class BattleRepository : IBattleRepository
    {
        private readonly IAmazonDynamoDB _dynamoDbClient;
        private Table? _encounterTable;
        private Table? _battleTable;
        private Table? _lootTable;

        public BattleRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _dynamoDbClient = dynamoDbClient;
        }

        private Table EncounterTable => _encounterTable ??= Table.LoadTable(_dynamoDbClient, AppSettings.EncountersTableName);
        private Table BattleTable => _battleTable ??= Table.LoadTable(_dynamoDbClient, AppSettings.BattlesTableName);
        private Table LootTable => _lootTable ??= Table.LoadTable(_dynamoDbClient, AppSettings.LootDropsTableName);

        public async Task<BossEncounter?> GetEncounterByIdAsync(string encounterId)
        {
            if (string.IsNullOrWhiteSpace(encounterId)) return null;

            var doc = await EncounterTable.GetItemAsync(encounterId);
            return doc != null ? JsonUtils.Deserialize<BossEncounter>(doc.ToJson()) : null;
        }

        public async Task SaveEncounterAsync(BossEncounter encounter)
        {
            if (encounter == null || string.IsNullOrWhiteSpace(encounter.encounterId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(encounter));
            await EncounterTable.PutItemAsync(doc);
        }

        public async Task<Battle?> GetBattleByIdAsync(string battleId)
        {
            if (string.IsNullOrWhiteSpace(battleId)) return null;

            var doc = await BattleTable.GetItemAsync(battleId);
            return doc != null ? JsonUtils.Deserialize<Battle>(doc.ToJson()) : null;
        }

        public async Task SaveBattleAsync(Battle battle)
        {
            if (battle == null || string.IsNullOrWhiteSpace(battle.battleId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(battle));
            await BattleTable.PutItemAsync(doc);
        }

        public async Task SaveLootDropAsync(LootDrop lootDrop)
        {
            if (lootDrop == null || string.IsNullOrWhiteSpace(lootDrop.lootId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(lootDrop));
            await LootTable.PutItemAsync(doc);
        }
    }
}
