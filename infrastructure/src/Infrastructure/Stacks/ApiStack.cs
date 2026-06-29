using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Constructs;

namespace Infrastructure.Stacks
{
    public class ApiStack : Stack
    {
        public ApiStack(Construct scope, string id, LambdaStack lambdaStack, CognitoStack cognitoStack, IStackProps? props = null) : base(scope, id, props)
        {
            var api = new RestApi(this, "GameApi", new RestApiProps
            {
                RestApiName = "RPG Game API",
                Description = "API Gateway cho AI Dungeon RPG Game",
                DefaultCorsPreflightOptions = new CorsOptions
                {
                    AllowOrigins = Cors.ALL_ORIGINS,
                    AllowMethods = Cors.ALL_METHODS,
                    AllowHeaders = new[] { "Content-Type", "Authorization" }
                }
            });

            // 1. Integrations
            var loginIntegration = new LambdaIntegration(lambdaStack.LoginFunction);
            var registerIntegration = new LambdaIntegration(lambdaStack.RegisterFunction);
            var confirmIntegration = new LambdaIntegration(lambdaStack.ConfirmSignUpFunction);
            var refreshIntegration = new LambdaIntegration(lambdaStack.RefreshTokenFunction);
            
            var getCharacterIntegration = new LambdaIntegration(lambdaStack.GetCharacterFunction);
            var createCharacterIntegration = new LambdaIntegration(lambdaStack.CreateCharacterFunction);
            var startStoryIntegration = new LambdaIntegration(lambdaStack.StartStoryFunction);
            var storyActionIntegration = new LambdaIntegration(lambdaStack.StoryActionFunction);
            var spawnBossIntegration = new LambdaIntegration(lambdaStack.SpawnBossFunction);
            var resolveBattleIntegration = new LambdaIntegration(lambdaStack.ResolveBattleFunction);
            var getInventoryIntegration = new LambdaIntegration(lambdaStack.GetInventoryFunction);

            // 2. Cognito Authorizer
            var authorizer = new CognitoUserPoolsAuthorizer(this, "GameCognitoAuthorizer", new CognitoUserPoolsAuthorizerProps
            {
                CognitoUserPools = new[] { cognitoStack.UserPool },
                AuthorizerName = "GameCognitoAuthorizer"
            });

            var authOptionsWithAuthorizer = new MethodOptions
            {
                Authorizer = authorizer,
                AuthorizationType = AuthorizationType.COGNITO
            };

            // 3. Define routes and bind integrations
            var authResource = api.Root.AddResource("auth");
            
            var loginResource = authResource.AddResource("login");
            loginResource.AddMethod("POST", loginIntegration); // Unprotected

            var registerResource = authResource.AddResource("register");
            registerResource.AddMethod("POST", registerIntegration); // Unprotected

            var confirmResource = authResource.AddResource("confirm");
            confirmResource.AddMethod("POST", confirmIntegration); // Unprotected

            var refreshResource = authResource.AddResource("refresh");
            refreshResource.AddMethod("POST", refreshIntegration); // Unprotected

            // Protected Routes
            var characterResource = api.Root.AddResource("character");
            characterResource.AddMethod("POST", createCharacterIntegration, authOptionsWithAuthorizer);

            var characterIdResource = characterResource.AddResource("{characterId}");
            characterIdResource.AddMethod("GET", getCharacterIntegration, authOptionsWithAuthorizer);

            var storyResource = api.Root.AddResource("story");
            var startStoryResource = storyResource.AddResource("start");
            startStoryResource.AddMethod("POST", startStoryIntegration, authOptionsWithAuthorizer);

            var actionStoryResource = storyResource.AddResource("action");
            actionStoryResource.AddMethod("POST", storyActionIntegration, authOptionsWithAuthorizer);

            var battleResource = api.Root.AddResource("battle");
            var spawnBossResource = battleResource.AddResource("spawn-boss");
            spawnBossResource.AddMethod("POST", spawnBossIntegration, authOptionsWithAuthorizer);

            var resolveBattleResource = battleResource.AddResource("resolve");
            resolveBattleResource.AddMethod("POST", resolveBattleIntegration, authOptionsWithAuthorizer);

            var inventoryResource = api.Root.AddResource("inventory");
            var inventoryCharacterIdResource = inventoryResource.AddResource("{characterId}");
            inventoryCharacterIdResource.AddMethod("GET", getInventoryIntegration, authOptionsWithAuthorizer);

            // Output API endpoint URL
            _ = new CfnOutput(this, "ApiUrl", new CfnOutputProps
            {
                Value = api.Url,
                Description = "API Gateway endpoint URL"
            });
        }
    }
}
