using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;
using Attribute = Amazon.CDK.AWS.DynamoDB.Attribute;

namespace Infrastructure.Stacks
{
    public class DatabaseStack : Stack
    {
        public ITable MainTable { get; }

        public ITable UsersTable => MainTable;
        public ITable CharactersTable => MainTable;
        public ITable BossEncountersTable => MainTable;
        public ITable BattlesTable => MainTable;
        public ITable StorySessionsTable => MainTable;
        public ITable StoryActionsTable => MainTable;
        public ITable InventoryTable => MainTable;

        public DatabaseStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            var table = new Table(this, "GameTable", new TableProps
            {
                TableName = "GameTable",
                PartitionKey = new Attribute { Name = "PK", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "SK", Type = AttributeType.STRING },
                BillingMode = BillingMode.PAY_PER_REQUEST,
                RemovalPolicy = RemovalPolicy.RETAIN,
                PointInTimeRecovery = true
            });

            // Global Secondary Index 1
            table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
            {
                IndexName = "GSI1",
                PartitionKey = new Attribute { Name = "GSI1PK", Type = AttributeType.STRING },
                SortKey = new Attribute { Name = "GSI1SK", Type = AttributeType.STRING },
                ProjectionType = ProjectionType.ALL
            });

            MainTable = table;
        }
    }
}
