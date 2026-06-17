using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameBackend.Handlers.Utils;
using GameBackend.Handlers.DependencyInjection;
using GameShared.DTOs.Battle;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace GameBackend.Handlers.Battle
{
    public class ResolveBattleHandler
    {
        private readonly IBattleService _battleService;

        public ResolveBattleHandler()
        {
            var sp = ServiceProviderBuilder.Build();
            _battleService = sp.GetRequiredService<IBattleService>();
        }

        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            if (request.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                return ResponseBuilder.Options();

            try
            {
                var resolveRequest = JsonSerializer.Deserialize<BattleResolveRequest>(request.Body);
                if (resolveRequest == null || string.IsNullOrWhiteSpace(resolveRequest.encounterId))
                    return ResponseBuilder.Error(400, "Invalid request payload", "INVALID_REQUEST");

                var result = await _battleService.ResolveBattleAsync(resolveRequest);
                return ResponseBuilder.Success(result);
            }
            catch (GameNotFoundException ex)
            {
                return ResponseBuilder.Error(404, ex.Message, "NOT_FOUND");
            }
            catch (GameValidationException ex)
            {
                return ResponseBuilder.Error(400, ex.Message, "VALIDATION_ERROR");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message} {ex.StackTrace}");
                return ResponseBuilder.Error(500, "Internal server error", "SERVER_ERROR");
            }
        }
    }
}
