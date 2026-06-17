using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Constructs;

namespace Infrastructure.Stacks
{
    public class ApiStack : Stack
    {
        public ApiStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
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

            // Route definitions (sẽ integrate với LambdaStack functions)
            // POST /auth/login → LoginFunction
            // POST /auth/register → RegisterFunction
            // GET  /character/{characterId} → GetCharacterFunction
            // POST /character → CreateCharacterFunction
            // POST /story/start → StartStoryFunction
            // POST /story/action → StoryActionFunction
            // POST /battle/spawn-boss → SpawnBossFunction
            // POST /battle/resolve → ResolveBattleFunction
            // GET  /inventory/{characterId} → GetInventoryFunction

            var auth = api.Root.AddResource("auth");
            auth.AddResource("login");
            auth.AddResource("register");

            var character = api.Root.AddResource("character");
            character.AddResource("{characterId}");

            var story = api.Root.AddResource("story");
            story.AddResource("start");
            story.AddResource("action");

            var battle = api.Root.AddResource("battle");
            battle.AddResource("spawn-boss");
            battle.AddResource("resolve");

            var inventory = api.Root.AddResource("inventory");
            inventory.AddResource("{characterId}");

            // Output API endpoint URL
            _ = new CfnOutput(this, "ApiUrl", new CfnOutputProps
            {
                Value = api.Url,
                Description = "API Gateway endpoint URL"
            });
        }
    }
}
