using BCrypt.Net;
using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameShared.DTOs.Auth;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
    /// <summary>
    /// AuthService — Plan A (Self-managed auth với BCrypt + JWT HS256).
    /// Plan B (Cognito): Thay bằng CognitoAuthService.cs.
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtHelper _jwtHelper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, JwtHelper jwtHelper, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _jwtHelper = jwtHelper;
            _logger = logger;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.username) || string.IsNullOrWhiteSpace(request.password))
                throw new GameValidationException("Username and password are required.");

            var user = await _userRepository.GetByUsernameAsync(request.username.Trim());
            if (user == null)
                throw new GameNotFoundException("User not found.");

            if (!VerifyPassword(request.password, user.passwordHash))
                throw new GameUnauthorizedException("Invalid credentials.");

            if (user.status != "Active")
                throw new GameUnauthorizedException("Account is disabled.");

            user.lastLoginAt = DateTime.UtcNow;
            await _userRepository.SaveAsync(user);

            _logger.LogInformation("User {Username} logged in successfully.", user.username);

            long expiresAt = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
            return new LoginResponse
            {
                token       = _jwtHelper.GenerateToken(user.userId, user.username),
                userId      = user.userId,
                displayName = user.displayName,
                expiresAt   = expiresAt
            };
        }

        public async Task<LoginResponse> RegisterAsync(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                throw new GameValidationException("Username, email, and password are required.");

            if (password.Length < 6)
                throw new GameValidationException("Password must be at least 6 characters.");

            // Check username duplicate
            var existingByUsername = await _userRepository.GetByUsernameAsync(username.Trim());
            if (existingByUsername != null)
                throw new GameConflictException("Username already exists.");

            // Check email duplicate
            var existingByEmail = await _userRepository.GetByEmailAsync(email.Trim().ToLowerInvariant());
            if (existingByEmail != null)
                throw new GameConflictException("Email already registered.");

            var user = new User
            {
                userId       = Guid.NewGuid().ToString("N"),
                username     = username.Trim(),
                email        = email.Trim().ToLowerInvariant(),
                passwordHash = HashPassword(password),
                displayName  = username.Trim(),
                status       = "Active",
                createdAt    = DateTime.UtcNow,
                lastLoginAt  = DateTime.UtcNow
            };

            await _userRepository.SaveAsync(user);
            _logger.LogInformation("User {Username} registered successfully.", user.username);

            long expiresAt = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
            return new LoginResponse
            {
                token       = _jwtHelper.GenerateToken(user.userId, user.username),
                userId      = user.userId,
                displayName = user.displayName,
                expiresAt   = expiresAt
            };
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            return Task.FromResult(_jwtHelper.IsTokenValid(token));
        }

        // ── Password (BCrypt) ─────────────────────────────────────

        /// <summary>Hash password dùng BCrypt với work factor 12.</summary>
        private static string HashPassword(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        /// <summary>Verify password so với BCrypt hash.</summary>
        private static bool VerifyPassword(string password, string hash)
        {
            try { return BCrypt.Net.BCrypt.Verify(password, hash); }
            catch { return false; }
        }
    }
}

