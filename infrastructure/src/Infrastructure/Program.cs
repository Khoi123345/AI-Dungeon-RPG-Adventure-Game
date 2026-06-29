using Amazon.CDK;

namespace Infrastructure
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            var env = new Amazon.CDK.Environment
            {
                Region = "ap-southeast-1" // Singapore region gần Việt Nam nhất
            };

            // Stack khởi tạo theo thứ tự dependency
            var dbStack = new Stacks.DatabaseStack(app, "GameDatabaseStack", new StackProps { Env = env });
            var cognitoStack = new Stacks.CognitoStack(app, "GameCognitoStack", new StackProps { Env = env });
            var lambdaStack = new Stacks.LambdaStack(app, "GameLambdaStack", dbStack, cognitoStack, new StackProps { Env = env });
            var apiStack = new Stacks.ApiStack(app, "GameApiStack", lambdaStack, cognitoStack, new StackProps { Env = env });
            var monitoringStack = new Stacks.MonitoringStack(app, "GameMonitoringStack", new StackProps { Env = env });

            // Khai báo dependency giữa các stack
            lambdaStack.AddDependency(dbStack);
            lambdaStack.AddDependency(cognitoStack);
            apiStack.AddDependency(lambdaStack);
            apiStack.AddDependency(cognitoStack);
            monitoringStack.AddDependency(lambdaStack);

            app.Synth();
        }
    }
}
