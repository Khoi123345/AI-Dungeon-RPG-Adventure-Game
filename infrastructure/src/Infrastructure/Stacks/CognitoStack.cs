using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Constructs;

namespace Infrastructure.Stacks
{
    public class CognitoStack : Stack
    {
        public UserPool UserPool { get; }
        public UserPoolClient UserPoolClient { get; }

        public CognitoStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            UserPool = new UserPool(this, "GameUserPool", new UserPoolProps
            {
                UserPoolName = "RPG-Game-User-Pool",
                SelfSignUpEnabled = true,
                AutoVerify = new AutoVerifiedAttrs { Email = true },
                PasswordPolicy = new PasswordPolicy
                {
                    MinLength = 8,
                    RequireDigits = true,
                    RequireLowercase = true,
                    RequireUppercase = false,
                    RequireSymbols = false
                },
                AccountRecovery = AccountRecovery.EMAIL_ONLY
            });

            UserPoolClient = UserPool.AddClient("GameMobileClient", new UserPoolClientOptions
            {
                UserPoolClientName = "GameMobileClient",
                AuthFlows = new AuthFlow { UserPassword = true },
                GenerateSecret = false // Mobile/Unity client không giữ client secret
            });

            new CfnOutput(this, "UserPoolId", new CfnOutputProps 
            { 
                Value = UserPool.UserPoolId,
                Description = "Cognito User Pool ID"
            });
            
            new CfnOutput(this, "UserPoolClientId", new CfnOutputProps 
            { 
                Value = UserPoolClient.UserPoolClientId,
                Description = "Cognito User Pool Client ID"
            });
        }
    }
}
