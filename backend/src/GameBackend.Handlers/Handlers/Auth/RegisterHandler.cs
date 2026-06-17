using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameBackend.Handlers.Utils;
using GameBackend.Handlers.DependencyInjection;
using GameShared.DTOs.Auth;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace GameBackend.Handlers.Auth
{
    public class RegisterHandler
    {
        private readonly IAuthService _authService;

        public RegisterHandler()
        {
            var sp = ServiceProviderBuilder.Build();
            _authService = sp.GetRequiredService<IAuthService>();
        }

        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            if (request.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                return ResponseBuilder.Options();

            try
            {
                var body = JsonSerializer.Deserialize<JsonElement>(request.Body);
                string username = body.GetProperty("username").GetString()!;
                string email = body.GetProperty("email").GetString()!;
                string password = body.GetProperty("password").GetString()!;

                var result = await _authService.RegisterAsync(username, email, password);
                return ResponseBuilder.Success(result, "Registration successful");
            }
            catch (GameConflictException ex)
            {
                return ResponseBuilder.Error(409, ex.Message, "USERNAME_EXISTS");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message}");
                return ResponseBuilder.Error(500, "Internal server error", "SERVER_ERROR");
            }
        }
    }
}
