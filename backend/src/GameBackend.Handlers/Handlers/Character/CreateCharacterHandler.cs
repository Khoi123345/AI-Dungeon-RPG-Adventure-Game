using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameBackend.Handlers.Utils;
using GameBackend.Handlers.DependencyInjection;
using GameShared.DTOs.Character;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace GameBackend.Handlers.Character
{
    public class CreateCharacterHandler
    {
        private readonly ICharacterService _characterService;

        public CreateCharacterHandler()
        {
            var sp = ServiceProviderBuilder.Build();
            _characterService = sp.GetRequiredService<ICharacterService>();
        }

        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            if (request.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                return ResponseBuilder.Options();

            try
            {
                var createRequest = JsonSerializer.Deserialize<CreateCharacterRequest>(request.Body);
                if (createRequest == null || string.IsNullOrWhiteSpace(createRequest.name))
                    return ResponseBuilder.Error(400, "Invalid request payload", "INVALID_REQUEST");

                var result = await _characterService.CreateCharacterAsync(createRequest);
                return ResponseBuilder.Success(result, "Character created");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message}");
                return ResponseBuilder.Error(500, "Internal server error", "SERVER_ERROR");
            }
        }
    }
}
