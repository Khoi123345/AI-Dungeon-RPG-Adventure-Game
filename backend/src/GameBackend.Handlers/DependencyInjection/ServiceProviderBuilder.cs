using System;
using System.IO;
using Amazon.DynamoDBv2;
using Amazon.CognitoIdentityProvider;
using GameBackend.Core.AIStory.Services;
using GameBackend.Core.AIStory.Services.Impl;
using GameBackend.Core.Repositories;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Services.Validation;
using GameBackend.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Amazon.BedrockRuntime;
using GameBackend.Core.Config;
using Microsoft.Extensions.Configuration;

namespace GameBackend.Handlers.DependencyInjection
{
    /// <summary>
    /// Setup DI container cho tất cả Lambda handlers.
    /// AmazonDynamoDBClient được đăng ký Singleton để tái sử dụng TCP connection
    /// giữa các lần invoke Lambda (tránh cold start overhead).
    /// </summary>
    public static class ServiceProviderBuilder
    {
        private static IServiceProvider? _serviceProvider;
        private static readonly object _lock = new();

        public static IServiceProvider Build()
        {
            if (_serviceProvider != null) return _serviceProvider;

            lock (_lock)
            {
                if (_serviceProvider != null) return _serviceProvider;

                var services = new ServiceCollection();

                // Configuration
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();

                services.AddSingleton<IConfiguration>(configuration);
                services.Configure<BedrockOptions>(configuration.GetSection("Bedrock"));

                // Logging
                services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information));

                // AWS Clients — Singleton để tái sử dụng TCP connection
                services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();
                services.AddSingleton<IAmazonCognitoIdentityProvider, AmazonCognitoIdentityProviderClient>();
                services.AddAWSService<IAmazonBedrockRuntime>();

                // Repositories
                services.AddSingleton<IUserRepository, UserRepository>();
                services.AddSingleton<ICharacterRepository, CharacterRepository>();
                services.AddSingleton<IBossRepository, BossRepository>();
                services.AddSingleton<IBattleRepository, BattleRepository>();
                services.AddSingleton<IStoryRepository, StoryRepository>();
                services.AddSingleton<IInventoryRepository, InventoryRepository>();
                services.AddSingleton<IContentService>(sp =>
                {
                    var contentRoot = Environment.GetEnvironmentVariable("CONTENT_ROOT") ?? Path.Combine(Directory.GetCurrentDirectory(), "Content");
                    return new ContentService(contentRoot);
                });

                // Utils
                services.AddSingleton<JwtHelper>();

                // Services
                bool useCognito = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("COGNITO_USER_POOL_ID"));
                if (useCognito)
                {
                    services.AddSingleton<IAuthService, CognitoAuthService>();
                }
                else
                {
                    services.AddSingleton<IAuthService, AuthService>();
                }
                services.AddSingleton<ICharacterService, CharacterService>();
                services.AddSingleton<IStoryStateUpdater, StoryStateUpdater>();
                services.AddSingleton<IGameRuleSubValidator, BossValidator>();
                services.AddSingleton<IGameRuleSubValidator, InventoryValidator>();
                services.AddSingleton<IGameRuleSubValidator, LocationValidator>();
                services.AddSingleton<IGameRuleSubValidator, CharacterValidator>();
                services.AddSingleton<IGameRuleSubValidator, StoryValidator>();
                services.AddSingleton<IGameRuleValidator, GameRuleValidator>();
                services.AddSingleton<IStoryService, StoryService>();
                services.AddSingleton<IBattleService, BattleService>();
                services.AddSingleton<IBedrockService, BedrockService>();
                services.AddSingleton<IStorySummaryService, StorySummaryService>();
                services.AddSingleton<IInventoryService, InventoryService>();

                _serviceProvider = services.BuildServiceProvider();
            }

            return _serviceProvider;
        }
    }
}
