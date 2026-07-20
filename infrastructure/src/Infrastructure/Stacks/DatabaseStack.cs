using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

namespace Infrastructure.Stacks
{
    public class DatabaseStack : Stack
    {
        public ITable UsersTable { get; }
        public ITable CharactersTable { get; }
        public ITable BossEncountersTable { get; }
        public ITable BattlesTable { get; }
        public ITable StorySessionsTable { get; }
        public ITable StoryActionsTable { get; }
        public ITable InventoryTable { get; }

        public DatabaseStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            UsersTable = CreateTable("GameUsers", "userId");
            CharactersTable = CreateTable("GameCharacters", "characterId");
            BossEncountersTable = CreateTable("GameBossEncounters", "encounterId");
            BattlesTable = CreateTable("GameBattles", "battleId");
            StorySessionsTable = CreateTable("GameStorySessions", "sessionId");
            StoryActionsTable = CreateTable("GameStoryActions", "actionId");
            InventoryTable = CreateTable("GameInventory", "inventoryId");

            // GSI cho Character lookup by userId
            (CharactersTable as Table)?.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "userId-index",
                PartitionKey = new Attribute { Name = "userId", Type = AttributeType.STRING }
            });

            // GSI cho StorySession lookup by characterId
            (StorySessionsTable as Table)?.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "characterId-index",
                PartitionKey = new Attribute { Name = "characterId", Type = AttributeType.STRING }
            });
        }

        private Table CreateTable(string tableName, string partitionKeyName)
        {
            return new Table(this, tableName, new TableProps
            {
                TableName = tableName,
                PartitionKey = new Attribute { Name = partitionKeyName, Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.RETAIN,
                PointInTimeRecovery = true
            });
        }
    }
}
