using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameShared.DTOs.Auth;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
    /// <summary>
    /// CognitoAuthService — Plan B.
    /// Delegate toàn bộ auth (password hash, token issue, email verify) cho AWS Cognito.
    /// Lambda chỉ làm intermediary: nhận request từ Unity → gọi Cognito SDK → trả token về client.
    ///
    /// DI swap: Trong ServiceProviderBuilder, thay IAuthService → CognitoAuthService (thay vì AuthService).
    /// </summary>
    public class CognitoAuthService : IAuthService
    {
        private readonly IAmazonCognitoIdentityProvider _cognitoClient;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<CognitoAuthService> _logger;
        private readonly string _userPoolId;
        private readonly string _clientId;

        public CognitoAuthService(
            IAmazonCognitoIdentityProvider cognitoClient,
            IUserRepository userRepository,
            ILogger<CognitoAuthService> logger)
        {
            _cognitoClient  = cognitoClient;
            _userRepository = userRepository;
            _logger         = logger;
            _userPoolId     = Environment.GetEnvironmentVariable("COGNITO_USER_POOL_ID") ?? throw new InvalidOperationException("COGNITO_USER_POOL_ID env var is required.");
            _clientId       = Environment.GetEnvironmentVariable("COGNITO_CLIENT_ID")    ?? throw new InvalidOperationException("COGNITO_CLIENT_ID env var is required.");
        }

        // ══════════════════════════════════════════════════════════════
        // LOGIN — USER_PASSWORD_AUTH flow
        // ══════════════════════════════════════════════════════════════

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var authRequest = new InitiateAuthRequest
                {
                    AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                    ClientId = _clientId,
                    AuthParameters = new Dictionary<string, string>
                    {
                        { "USERNAME", request.username.Trim() },
                        { "PASSWORD", request.password }
                    }
                };

                var authResponse = await _cognitoClient.InitiateAuthAsync(authRequest);

                if (authResponse.AuthenticationResult == null)
                    throw new GameUnauthorizedException("Authentication failed.");

                var tokens = authResponse.AuthenticationResult;

                // Map Cognito user → game user profile trong DynamoDB
                var user = await GetOrCreateGameUser(request.username.Trim(), tokens);

                _logger.LogInformation("Cognito login success for {Username}.", request.username);

                return new LoginResponse
                {
                    token        = tokens.AccessToken,
                    idToken      = tokens.IdToken,
                    refreshToken = tokens.RefreshToken,
                    userId       = user.userId,
                    displayName  = user.displayName,
                    expiresAt    = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn).ToUnixTimeSeconds()
                };
            }
            catch (NotAuthorizedException)
            {
                throw new GameUnauthorizedException("Invalid credentials.");
            }
            catch (UserNotFoundException)
            {
                throw new GameNotFoundException("User not found.");
            }
            catch (UserNotConfirmedException)
            {
                // Client sẽ redirect sang màn hình xác nhận OTP
                throw new GameUnauthorizedException("USER_NOT_CONFIRMED");
            }
        }

        // ══════════════════════════════════════════════════════════════
        // REGISTER — SignUp flow
        // ══════════════════════════════════════════════════════════════

        public async Task<LoginResponse> RegisterAsync(string username, string email, string password)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            // ── Check email đã tồn tại trong Cognito chưa (nguồn truth duy nhất) ──
            // DynamoDB không dùng được vì email được lưu là string.Empty trong profile.
            bool emailTaken = await IsEmailAlreadyRegisteredInCognitoAsync(normalizedEmail);
            if (emailTaken)
                throw new GameConflictException("Email already registered.");

            try
            {
                var signUpRequest = new SignUpRequest
                {
                    ClientId = _clientId,
                    Username = username.Trim(),
                    Password = password,
                    UserAttributes = new List<AttributeType>
                    {
                        new AttributeType { Name = "email", Value = normalizedEmail }
                    }
                };

                await _cognitoClient.SignUpAsync(signUpRequest);

                _logger.LogInformation("Cognito SignUp success for {Username}. Awaiting confirmation.", username);

                // Trả về response rỗng — client cần xác nhận email trước khi login
                // Field errorCode = "USER_NOT_CONFIRMED" báo client cần redirect sang màn hình OTP
                return new LoginResponse
                {
                    errorCode = "USER_NOT_CONFIRMED"
                };
            }
            catch (UsernameExistsException)
            {
                throw new GameConflictException("Username already exists.");
            }
            catch (AliasExistsException)
            {
                // Phòng thủ thêm nếu Cognito User Pool bật email alias unique
                throw new GameConflictException("Email already registered.");
            }
            catch (InvalidPasswordException ex)
            {
                throw new GameValidationException($"Password policy violation: {ex.Message}");
            }
        }

        /// <summary>
        /// Hỏi Cognito xem email đã có user nào dùng chưa.
        /// Dùng ListUsers với filter vì đây là nguồn truth — DynamoDB lưu email = string.Empty.
        /// </summary>
        private async Task<bool> IsEmailAlreadyRegisteredInCognitoAsync(string email)
        {
            try
            {
                var request = new ListUsersRequest
                {
                    UserPoolId = _userPoolId,
                    Filter = $"email = \"{email}\"",
                    Limit = 1
                };
                var response = await _cognitoClient.ListUsersAsync(request);
                return response.Users != null && response.Users.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[CognitoAuth] ListUsers failed (email check skipped): {Msg}", ex.Message);
                // Nếu ListUsers lỗi (permissions, network...) → cho phép qua,
                // Cognito SignUpAsync sẽ là lớp bảo vệ cuối cùng.
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════════
        // VALIDATE TOKEN — API Gateway Cognito Authorizer làm việc này
        // ══════════════════════════════════════════════════════════════

        public Task<bool> ValidateTokenAsync(string token)
        {
            // Plan B: Token validation do API Gateway Cognito Authorizer xử lý.
            // Lambda chỉ nhận request đã được validate. Method này chỉ cho backward compat.
            return Task.FromResult(!string.IsNullOrWhiteSpace(token));
        }

        // ══════════════════════════════════════════════════════════════
        // CONFIRM SIGN UP — OTP từ email
        // ══════════════════════════════════════════════════════════════

        public async Task ConfirmSignUpAsync(string username, string confirmationCode)
        {
            var confirmRequest = new Amazon.CognitoIdentityProvider.Model.ConfirmSignUpRequest
            {
                ClientId         = _clientId,
                Username         = username.Trim(),
                ConfirmationCode = confirmationCode.Trim()
            };

            await _cognitoClient.ConfirmSignUpAsync(confirmRequest);
            _logger.LogInformation("Cognito ConfirmSignUp success for {Username}.", username);
        }

        // ══════════════════════════════════════════════════════════════
        // REFRESH TOKEN
        // ══════════════════════════════════════════════════════════════

        public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
        {
            var authRequest = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                ClientId = _clientId,
                AuthParameters = new Dictionary<string, string>
                {
                    { "REFRESH_TOKEN", refreshToken }
                }
            };

            var authResponse = await _cognitoClient.InitiateAuthAsync(authRequest);
            var tokens = authResponse.AuthenticationResult;

            return new LoginResponse
            {
                token        = tokens.AccessToken,
                idToken      = tokens.IdToken,
                refreshToken = tokens.RefreshToken ?? refreshToken, // Cognito có thể không gửi lại refresh token
                expiresAt    = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn).ToUnixTimeSeconds()
            };
        }

        // ══════════════════════════════════════════════════════════════
        // PRIVATE: Map Cognito User → DynamoDB Game Profile
        // ══════════════════════════════════════════════════════════════

        /// <summary>
        /// Sau khi Cognito xác thực thành công, tìm hoặc tạo game user profile trong DynamoDB.
        /// Lần đầu login: tạo mới. Những lần sau: load profile hiện có.
        /// </summary>
        private async Task<User> GetOrCreateGameUser(string username, AuthenticationResultType tokens)
        {
            // Lấy Cognito sub từ ID token (sub claim)
            string? cognitoSub = GetSubFromIdToken(tokens.IdToken);

            User? user = null;

            if (!string.IsNullOrEmpty(cognitoSub))
                user = await _userRepository.GetByCognitoSubAsync(cognitoSub);

            if (user == null)
                user = await _userRepository.GetByUsernameAsync(username);

            if (user != null)
            {
                // Cập nhật lastLogin
                user.lastLoginAt = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(cognitoSub)) user.cognitoSub = cognitoSub;
                await _userRepository.SaveAsync(user);
                return user;
            }

            // Tạo mới game profile cho Cognito user
            // Lấy email từ ID token để lưu vào DynamoDB (tránh lưu string.Empty)
            string emailFromToken = GetEmailFromIdToken(tokens.IdToken) ?? string.Empty;

            var newUser = new User
            {
                userId      = Guid.NewGuid().ToString("N"),
                username    = username,
                email       = emailFromToken,
                cognitoSub  = cognitoSub ?? string.Empty,
                displayName = username,
                status      = "Active",
                createdAt   = DateTime.UtcNow,
                lastLoginAt = DateTime.UtcNow
            };

            await _userRepository.SaveAsync(newUser);
            _logger.LogInformation("Created new game profile for Cognito user {Username} (email={Email}).", username, emailFromToken);
            return newUser;
        }

        private static string? GetSubFromIdToken(string idToken)
            => GetClaimFromIdToken(idToken, "sub");

        private static string? GetEmailFromIdToken(string idToken)
            => GetClaimFromIdToken(idToken, "email");

        /// <summary>Giải mã JWT payload và lấy claim theo tên.</summary>
        private static string? GetClaimFromIdToken(string idToken, string claimName)
        {
            try
            {
                string[] parts = idToken.Split('.');
                if (parts.Length != 3) return null;
                string payload = parts[1]
                    .Replace('-', '+')
                    .Replace('_', '/');
                int padding = (4 - payload.Length % 4) % 4;
                payload += new string('=', padding);
                string json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty(claimName, out var val) ? val.GetString() : null;
            }
            catch { return null; }
        }
    }
}
