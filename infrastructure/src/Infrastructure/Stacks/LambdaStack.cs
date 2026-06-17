using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace Infrastructure.Stacks
{
    public class LambdaStack : Stack
    {
        public Function LoginFunction { get; }
        public Function RegisterFunction { get; }
        public Function GetCharacterFunction { get; }
        public Function CreateCharacterFunction { get; }
        public Function StartStoryFunction { get; }
        public Function StoryActionFunction { get; }
        public Function SpawnBossFunction { get; }
        public Function ResolveBattleFunction { get; }
        public Function GetInventoryFunction { get; }

        public LambdaStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            var commonProps = new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                MemorySize = 512,
                Timeout = Duration.Seconds(29),
                Code = Code.FromAsset("../backend/src/GameBackend.Handlers/bin/Release/net8.0/publish"),
                Environment = new Dictionary<string, string>
                {
                    { "USERS_TABLE", "GameUsers" },
                    { "CHARACTERS_TABLE", "GameCharacters" },
                    { "ENCOUNTERS_TABLE", "GameBossEncounters" },
                    { "BATTLES_TABLE", "GameBattles" },
                    { "STORY_SESSIONS_TABLE", "GameStorySessions" },
                    { "STORY_ACTIONS_TABLE", "GameStoryActions" },
                    { "INVENTORY_TABLE", "GameInventory" }
                }
            };

            // Auth — Critical functions: bật SnapStart
            LoginFunction = CreateFunction("LoginFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Auth.LoginHandler::Handler",
                commonProps, enableSnapStart: true);

            RegisterFunction = CreateFunction("RegisterFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Auth.RegisterHandler::Handler",
                commonProps);

            // Character
            GetCharacterFunction = CreateFunction("GetCharacterFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Character.GetCharacterHandler::Handler",
                commonProps);

            CreateCharacterFunction = CreateFunction("CreateCharacterFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Character.CreateCharacterHandler::Handler",
                commonProps);

            // Story — Bedrock AI call: timeout cao hơn, memory nhiều hơn
            var storyProps = new FunctionProps
            {
                Runtime = commonProps.Runtime,
                MemorySize = 1024,
                Timeout = Duration.Seconds(29),
                Code = commonProps.Code,
                Environment = commonProps.Environment
            };

            StartStoryFunction = CreateFunction("StartStoryFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Story.StartStoryHandler::Handler",
                storyProps);

            StoryActionFunction = CreateFunction("StoryActionFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Story.StoryActionHandler::Handler",
                storyProps);

            // Battle — Critical: bật SnapStart
            SpawnBossFunction = CreateFunction("SpawnBossFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Battle.SpawnBossHandler::Handler",
                commonProps, enableSnapStart: true);

            ResolveBattleFunction = CreateFunction("ResolveBattleFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Battle.ResolveBattleHandler::Handler",
                commonProps, enableSnapStart: true);

            // Inventory
            GetInventoryFunction = CreateFunction("GetInventoryFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Inventory.GetInventoryHandler::Handler",
                commonProps);
        }

        private Function CreateFunction(string name, string handler, FunctionProps baseProps, bool enableSnapStart = false)
        {
            var fn = new Function(this, name, new FunctionProps
            {
                FunctionName = name,
                Runtime = baseProps.Runtime,
                Handler = handler,
                MemorySize = baseProps.MemorySize,
                Timeout = baseProps.Timeout,
                Code = baseProps.Code,
                Environment = baseProps.Environment,
                SnapStart = enableSnapStart ? SnapStartConf.ON_PUBLISHED_VERSIONS : null
            });

            return fn;
        }
    }
}
