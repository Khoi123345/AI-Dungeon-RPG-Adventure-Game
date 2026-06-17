using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameBackend.Handlers.Utils;
using GameBackend.Handlers.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace GameBackend.Handlers.Inventory
{
    public class GetInventoryHandler
    {
        private readonly IInventoryService _inventoryService;

        public GetInventoryHandler()
        {
            var sp = ServiceProviderBuilder.Build();
            _inventoryService = sp.GetRequiredService<IInventoryService>();
        }

        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            if (request.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                return ResponseBuilder.Options();

            try
            {
                string? characterId = null;
                request.PathParameters?.TryGetValue("characterId", out characterId);
                if (string.IsNullOrWhiteSpace(characterId))
                    return ResponseBuilder.Error(400, "characterId is required", "INVALID_REQUEST");

                var result = await _inventoryService.GetInventoryAsync(characterId);
                return ResponseBuilder.Success(result);
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message}");
                return ResponseBuilder.Error(500, "Internal server error", "SERVER_ERROR");
            }
        }
    }
}
