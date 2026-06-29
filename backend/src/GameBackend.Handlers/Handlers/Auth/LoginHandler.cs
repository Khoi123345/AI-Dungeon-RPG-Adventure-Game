using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameBackend.Handlers.Utils;
using GameBackend.Handlers.DependencyInjection;
using GameShared.DTOs.Auth;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GameBackend.Handlers.Auth
{
    /// <summary>
    /// Lambda entrypoint cho POST /auth/login.
    /// Handler chỉ parse request, gọi Service, trả response.
    /// KHÔNG chứa business logic.
    /// </summary>
    public class LoginHandler
    {
        private readonly IAuthService _authService;

        public LoginHandler()
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
                var loginRequest = JsonUtils.Deserialize<LoginRequest>(request.Body);
                if (loginRequest == null || string.IsNullOrWhiteSpace(loginRequest.username))
                    return ResponseBuilder.Error(400, "Invalid request payload", "INVALID_REQUEST");

                var result = await _authService.LoginAsync(loginRequest);
                return ResponseBuilder.Success(result, "Login successful");
            }
            catch (GameNotFoundException ex)
            {
                return ResponseBuilder.Error(404, ex.Message, "USER_NOT_FOUND");
            }
            catch (GameUnauthorizedException ex)
            {
                return ResponseBuilder.Error(401, ex.Message, "INVALID_CREDENTIALS");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message} {ex.StackTrace}");
                return ResponseBuilder.Error(500, "Internal server error", "SERVER_ERROR");
            }
        }
    }
}
