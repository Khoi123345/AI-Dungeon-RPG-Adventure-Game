using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameBackend.Handlers.Utils;
using GameBackend.Handlers.DependencyInjection;
using GameShared.DTOs.Inventory;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace GameBackend.Handlers.Inventory
{
    /// <summary>Lambda handler cho POST /inventory/{characterId}/unequip</summary>
    public class UnequipItemHandler
    {
        private readonly IInventoryService _inventoryService;

        public UnequipItemHandler()
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
                    return ResponseBuilder.Error(400, "characterId là bắt buộc.", "INVALID_REQUEST");

                var body = JsonSerializer.Deserialize<UnequipItemRequest>(
                    request.Body ?? "{}",
                    new JsonSerializerOptions { IncludeFields = true, PropertyNameCaseInsensitive = true });

                if (body == null || string.IsNullOrWhiteSpace(body.inventoryId))
                    return ResponseBuilder.Error(400, "inventoryId là bắt buộc.", "INVALID_REQUEST");

                var result = await _inventoryService.UnequipItemAsync(characterId, body.inventoryId);
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
                context.Logger.LogLine($"[UnequipItemHandler] Error: {ex.Message} {ex.StackTrace}");
                return ResponseBuilder.Error(500, "Internal server error", "SERVER_ERROR");
            }
        }
    }
}
