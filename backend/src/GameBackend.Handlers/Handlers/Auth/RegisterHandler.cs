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
    /// <summary>
    /// Lambda entrypoint cho POST /auth/register.
    /// Dùng RegisterRequest DTO từ shared để đồng nhất schema giữa client và server.
    /// </summary>
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
                var registerRequest = JsonSerializer.Deserialize<RegisterRequest>(request.Body);

                if (registerRequest == null ||
                    string.IsNullOrWhiteSpace(registerRequest.username) ||
                    string.IsNullOrWhiteSpace(registerRequest.email) ||
                    string.IsNullOrWhiteSpace(registerRequest.password))
                {
                    return ResponseBuilder.Error(400, "Username, email, and password are required.", "INVALID_REQUEST");
                }

                if (!string.IsNullOrEmpty(registerRequest.confirmPassword) &&
                    registerRequest.password != registerRequest.confirmPassword)
                {
                    return ResponseBuilder.Error(400, "Passwords do not match.", "PASSWORD_MISMATCH");
                }

                var result = await _authService.RegisterAsync(
                    registerRequest.username,
                    registerRequest.email,
                    registerRequest.password);

                return ResponseBuilder.Success(result, "Registration successful");
            }
            catch (GameConflictException ex)
            {
                // Phân biệt username vs email conflict
                string errorCode = ex.Message.Contains("Email") ? "EMAIL_EXISTS" : "USERNAME_EXISTS";
                return ResponseBuilder.Error(409, ex.Message, errorCode);
            }
            catch (GameValidationException ex)
            {
                return ResponseBuilder.Error(400, ex.Message, "VALIDATION_ERROR");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"RegisterHandler Error: {ex.Message} {ex.StackTrace}");
                return ResponseBuilder.Error(500, "Internal server error", "SERVER_ERROR");
            }
        }
    }
}

