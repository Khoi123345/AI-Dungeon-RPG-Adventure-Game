using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameBackend.Handlers.Utils;
using GameBackend.Handlers.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace GameBackend.Handlers.Inventory
{
    /// <summary>Lambda handler cho GET /items/{itemId}</summary>
    public class GetItemDetailHandler
    {
        private readonly IInventoryService _inventoryService;

        public GetItemDetailHandler()
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
                string? itemId = null;
                request.PathParameters?.TryGetValue("itemId", out itemId);
                if (string.IsNullOrWhiteSpace(itemId))
                    return ResponseBuilder.Error(400, "itemId là bắt buộc.", "INVALID_REQUEST");

                var result = await _inventoryService.GetItemDetailAsync(itemId);
                if (result == null)
                    return ResponseBuilder.Error(404, $"Không tìm thấy item '{itemId}'.", "NOT_FOUND");

                return ResponseBuilder.Success(result);
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"[GetItemDetailHandler] Error: {ex.Message}");
                return ResponseBuilder.Error(500, "Internal server error", "SERVER_ERROR");
            }
        }
    }
}
