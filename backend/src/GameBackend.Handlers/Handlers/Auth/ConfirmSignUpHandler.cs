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
    /// Lambda entrypoint cho POST /auth/confirm.
    /// Xác nhận OTP code sau khi đăng ký (Plan B — Cognito).
    /// </summary>
    public class ConfirmSignUpHandler
    {
        private readonly IAuthService _authService;

        public ConfirmSignUpHandler()
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
                var confirmRequest = JsonUtils.Deserialize<ConfirmSignUpRequest>(request.Body);
                if (confirmRequest == null || string.IsNullOrWhiteSpace(confirmRequest.username) || string.IsNullOrWhiteSpace(confirmRequest.confirmationCode))
                {
                    return ResponseBuilder.Error(400, "Username and confirmation code are required.", "INVALID_REQUEST");
                }

                await _authService.ConfirmSignUpAsync(confirmRequest.username, confirmRequest.confirmationCode);
                return ResponseBuilder.Success<object?>(null, "Account confirmed successfully.");
            }
            catch (Amazon.CognitoIdentityProvider.Model.ExpiredCodeException)
            {
                return ResponseBuilder.Error(400, "The verification code has expired.", "CODE_EXPIRED");
            }
            catch (Amazon.CognitoIdentityProvider.Model.CodeMismatchException)
            {
                return ResponseBuilder.Error(400, "Invalid verification code.", "CODE_MISMATCH");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"ConfirmSignUpHandler Error: {ex.Message} {ex.StackTrace}");
                return ResponseBuilder.Error(500, "Internal server error", "SERVER_ERROR");
            }
        }
    }
}
