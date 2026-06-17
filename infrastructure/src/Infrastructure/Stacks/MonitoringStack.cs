using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch;
using Constructs;

namespace Infrastructure.Stacks
{
    public class MonitoringStack : Stack
    {
        public MonitoringStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
        {
            // Dashboard cho game backend
            var dashboard = new Dashboard(this, "GameDashboard", new DashboardProps
            {
                DashboardName = "RPG-Game-Backend"
            });

            // Lambda error alarm cho tất cả functions
            var lambdaFunctions = new[]
            {
                "LoginFunction", "RegisterFunction",
                "GetCharacterFunction", "CreateCharacterFunction",
                "StartStoryFunction", "StoryActionFunction",
                "SpawnBossFunction", "ResolveBattleFunction",
                "GetInventoryFunction"
            };

            foreach (var functionName in lambdaFunctions)
            {
                var errorMetric = new Metric(new MetricProps
                {
                    Namespace = "AWS/Lambda",
                    MetricName = "Errors",
                    DimensionsMap = new Dictionary<string, string>
                    {
                        { "FunctionName", functionName }
                    },
                    Statistic = "Sum",
                    Period = Duration.Minutes(5)
                });

                new Alarm(this, $"{functionName}ErrorAlarm", new AlarmProps
                {
                    AlarmName = $"RPG-{functionName}-Errors",
                    Metric = errorMetric,
                    Threshold = 5,
                    EvaluationPeriods = 1,
                    ComparisonOperator = ComparisonOperator.GREATER_THAN_OR_EQUAL_TO_THRESHOLD
                });
            }
        }
    }
}
