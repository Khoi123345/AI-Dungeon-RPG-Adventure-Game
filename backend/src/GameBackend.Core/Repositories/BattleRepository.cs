using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.Models;
using System.Threading.Tasks;
using GameBackend.Core.Utils;

namespace GameBackend.Core.Repositories
{
    public class BattleRepository : IBattleRepository
    {
        private readonly Table _table;

        public BattleRepository(IAmazonDynamoDB dynamoDbClient)
        {
            _table = Table.LoadTable(dynamoDbClient, AppSettings.GameTableName);
        }

        public async Task<BossEncounter?> GetEncounterByIdAsync(string encounterId)
        {
            if (string.IsNullOrWhiteSpace(encounterId)) return null;

            var doc = await _table.GetItemAsync($"ENCOUNTER#{encounterId}", "METADATA");
            return doc != null ? JsonUtils.Deserialize<BossEncounter>(doc.ToJson()) : null;
        }

        public async Task SaveEncounterAsync(BossEncounter encounter)
        {
            if (encounter == null || string.IsNullOrWhiteSpace(encounter.encounterId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(encounter));
            doc["PK"] = $"ENCOUNTER#{encounter.encounterId}";
            doc["SK"] = "METADATA";
            doc["GSI1PK"] = $"CHAR#{encounter.characterId}";
            doc["GSI1SK"] = $"ENCOUNTER#{encounter.encounterId}";

            await _table.PutItemAsync(doc);
        }

        public async Task<Battle?> GetBattleByIdAsync(string battleId)
        {
            if (string.IsNullOrWhiteSpace(battleId)) return null;

            var doc = await _table.GetItemAsync($"BATTLE#{battleId}", "METADATA");
            return doc != null ? JsonUtils.Deserialize<Battle>(doc.ToJson()) : null;
        }

        public async Task SaveBattleAsync(Battle battle)
        {
            if (battle == null || string.IsNullOrWhiteSpace(battle.battleId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(battle));
            doc["PK"] = $"BATTLE#{battle.battleId}";
            doc["SK"] = "METADATA";

            await _table.PutItemAsync(doc);
        }

        public async Task SaveLootDropAsync(LootDrop lootDrop)
        {
            if (lootDrop == null || string.IsNullOrWhiteSpace(lootDrop.lootId)) return;

            var doc = Document.FromJson(JsonUtils.Serialize(lootDrop));
            doc["PK"] = $"LOOT#{lootDrop.lootId}";
            doc["SK"] = "METADATA";

            await _table.PutItemAsync(doc);
        }
    }
}
