using Amazon.DynamoDBv2;
using GameBackend.Core.Repositories;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Services.Validation;
using GameBackend.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

                // Logging
                services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

                // AWS Clients — Singleton để tái sử dụng TCP connection
                services.AddSingleton<IAmazonDynamoDB, AmazonDynamoDBClient>();

                // Repositories
                services.AddSingleton<IUserRepository, UserRepository>();
                services.AddSingleton<ICharacterRepository, CharacterRepository>();
                services.AddSingleton<IBossRepository, BossRepository>();
                services.AddSingleton<IBattleRepository, BattleRepository>();
                services.AddSingleton<IStoryRepository, StoryRepository>();
                services.AddSingleton<IInventoryRepository, InventoryRepository>();
                services.AddSingleton<IItemRepository, InMemoryItemRepository>();
                services.AddSingleton<ILocationRepository, InMemoryLocationRepository>();
                services.AddSingleton<ILootRepository, InMemoryLootRepository>();

                // Utils
                services.AddSingleton<JwtHelper>();

                // Services
                services.AddSingleton<IAuthService, AuthService>();
                services.AddSingleton<ICharacterService, CharacterService>();
                services.AddSingleton<IStoryStateUpdater, StoryStateUpdater>();
                services.AddSingleton<IGameRuleSubValidator, BossValidator>();
                services.AddSingleton<IGameRuleSubValidator, InventoryValidator>();
                services.AddSingleton<IGameRuleSubValidator, LocationValidator>();
                services.AddSingleton<IGameRuleSubValidator, QuestValidator>();
                services.AddSingleton<IGameRuleSubValidator, CharacterValidator>();
                services.AddSingleton<IGameRuleSubValidator, StoryValidator>();
                services.AddSingleton<IGameRuleValidator, GameRuleValidator>();
                services.AddSingleton<IStoryService, StoryService>();
                services.AddSingleton<IBattleService, BattleService>();
                services.AddSingleton<IBedrockService, BedrockService>();
                services.AddSingleton<IInventoryService, InventoryService>();

                _serviceProvider = services.BuildServiceProvider();
            }

            return _serviceProvider;
        }
    }
}
