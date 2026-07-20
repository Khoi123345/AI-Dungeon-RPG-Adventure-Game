using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Constructs;

namespace Infrastructure.Stacks
{
    public class LambdaStack : Stack
    {
        public Function LoginFunction { get; }
        public Function RegisterFunction { get; }
        public Function ConfirmSignUpFunction { get; }
        public Function RefreshTokenFunction { get; }
        public Function GetCharacterFunction { get; }
        public Function CreateCharacterFunction { get; }
        public Function StartStoryFunction { get; }
        public Function StoryActionFunction { get; }
        public Function SpawnBossFunction { get; }
        public Function ResolveBattleFunction { get; }
        public Function GetInventoryFunction { get; }

        public LambdaStack(Construct scope, string id, DatabaseStack dbStack, CognitoStack cognitoStack, IStackProps? props = null) : base(scope, id, props)
        {
            var commonProps = new FunctionProps
            {
                Runtime = Runtime.DOTNET_8,
                MemorySize = 512,
                Timeout = Duration.Seconds(29),
                Code = Code.FromAsset("../backend/src/GameBackend.Handlers/bin/Release/net8.0/publish"),
                Environment = new Dictionary<string, string>
                {
                    { "USERS_TABLE", dbStack.UsersTable.TableName },
                    { "CHARACTERS_TABLE", dbStack.CharactersTable.TableName },
                    { "ENCOUNTERS_TABLE", dbStack.BossEncountersTable.TableName },
                    { "BATTLES_TABLE", dbStack.BattlesTable.TableName },
                    { "STORY_SESSIONS_TABLE", dbStack.StorySessionsTable.TableName },
                    { "STORY_ACTIONS_TABLE", dbStack.StoryActionsTable.TableName },
                    { "INVENTORY_TABLE", dbStack.InventoryTable.TableName },
                    { "COGNITO_USER_POOL_ID", cognitoStack.UserPool.UserPoolId },
                    { "COGNITO_CLIENT_ID", cognitoStack.UserPoolClient.UserPoolClientId }
                }
            };

            // Auth — Critical functions: tắt SnapStart vì dotnet8 chưa hỗ trợ
            LoginFunction = CreateFunction("LoginFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Auth.LoginHandler::Handler",
                commonProps);

            RegisterFunction = CreateFunction("RegisterFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Auth.RegisterHandler::Handler",
                commonProps);

            ConfirmSignUpFunction = CreateFunction("ConfirmSignUpFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Auth.ConfirmSignUpHandler::Handler",
                commonProps);

            RefreshTokenFunction = CreateFunction("RefreshTokenFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Auth.RefreshTokenHandler::Handler",
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

            // Battle — Critical: tắt SnapStart vì dotnet8 chưa hỗ trợ
            SpawnBossFunction = CreateFunction("SpawnBossFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Battle.SpawnBossHandler::Handler",
                commonProps);

            ResolveBattleFunction = CreateFunction("ResolveBattleFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Battle.ResolveBattleHandler::Handler",
                commonProps);

            // Inventory
            GetInventoryFunction = CreateFunction("GetInventoryFunction",
                "GameBackend.Handlers::GameBackend.Handlers.Inventory.GetInventoryHandler::Handler",
                commonProps);

            // Grant DynamoDB Permissions
            dbStack.UsersTable.GrantReadWriteData(LoginFunction);
            dbStack.UsersTable.GrantReadWriteData(RegisterFunction);
            dbStack.UsersTable.GrantReadWriteData(ConfirmSignUpFunction);
            dbStack.UsersTable.GrantReadWriteData(RefreshTokenFunction);

            dbStack.CharactersTable.GrantReadData(GetCharacterFunction);
            dbStack.CharactersTable.GrantReadWriteData(CreateCharacterFunction);
            dbStack.UsersTable.GrantReadData(GetCharacterFunction);

            dbStack.StorySessionsTable.GrantReadWriteData(StartStoryFunction);
            dbStack.StorySessionsTable.GrantReadWriteData(StoryActionFunction);
            dbStack.StoryActionsTable.GrantReadWriteData(StoryActionFunction);
            dbStack.CharactersTable.GrantReadWriteData(StoryActionFunction);

            dbStack.BossEncountersTable.GrantReadWriteData(SpawnBossFunction);
            dbStack.BattlesTable.GrantReadWriteData(ResolveBattleFunction);
            dbStack.InventoryTable.GrantReadData(GetInventoryFunction);

            // Grant Cognito Permissions
            cognitoStack.UserPool.Grant(LoginFunction, "cognito-idp:InitiateAuth");
            cognitoStack.UserPool.Grant(RegisterFunction, "cognito-idp:SignUp");
            cognitoStack.UserPool.Grant(ConfirmSignUpFunction, "cognito-idp:ConfirmSignUp");
            cognitoStack.UserPool.Grant(RefreshTokenFunction, "cognito-idp:InitiateAuth");
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
