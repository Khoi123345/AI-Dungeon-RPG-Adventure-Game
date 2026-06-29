using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameBackend.Handlers.Utils;
using GameBackend.Handlers.DependencyInjection;
using GameShared.DTOs.Auth;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace GameBackend.Handlers.Auth
{
    /// <summary>
    /// Lambda entrypoint cho POST /auth/refresh.
    /// Refresh Access Token bằng Refresh Token (Plan B — Cognito).
    /// </summary>
    public class RefreshTokenHandler
    {
        private readonly IAuthService _authService;

        public RefreshTokenHandler()
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
                var refreshRequest = JsonUtils.Deserialize<RefreshTokenRequest>(request.Body);
                if (refreshRequest == null || string.IsNullOrWhiteSpace(refreshRequest.refreshToken))
                {
                    return ResponseBuilder.Error(400, "Refresh token is required.", "INVALID_REQUEST");
                }

                var result = await _authService.RefreshTokenAsync(refreshRequest.refreshToken);
                return ResponseBuilder.Success(result, "Token refreshed successfully.");
            }
            catch (Amazon.CognitoIdentityProvider.Model.NotAuthorizedException)
            {
                return ResponseBuilder.Error(401, "Invalid or expired refresh token.", "INVALID_REFRESH_TOKEN");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"RefreshTokenHandler Error: {ex.Message} {ex.StackTrace}");
                return ResponseBuilder.Error(500, "Internal server error", "SERVER_ERROR");
            }
        }
    }
}
