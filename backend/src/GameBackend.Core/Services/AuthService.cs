using GameBackend.Core.Repositories.Interfaces;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameShared.DTOs.Auth;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
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
            var user = await _userRepository.GetByUsernameAsync(request.username);
            if (user == null)
            {
                throw new GameNotFoundException("User not found");
            }

            if (!VerifyPassword(request.password, user.passwordHash))
            {
                throw new GameUnauthorizedException("Invalid credentials");
            }

            user.lastLoginAt = DateTime.UtcNow;
            await _userRepository.SaveAsync(user);

            return new LoginResponse
            {
                token = _jwtHelper.GenerateToken(user.userId, user.username),
                userId = user.userId,
                displayName = user.displayName,
                expiresAt = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds()
            };
        }

        public async Task<LoginResponse> RegisterAsync(string username, string email, string password)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(username);
            if (existingUser != null)
            {
                throw new GameConflictException("Username already exists");
            }

            var user = new User
            {
                userId = Guid.NewGuid().ToString("N"),
                username = username,
                email = email,
                passwordHash = HashPassword(password),
                displayName = username,
                status = "Active",
                createdAt = DateTime.UtcNow,
                lastLoginAt = DateTime.UtcNow
            };

            await _userRepository.SaveAsync(user);

            return new LoginResponse
            {
                token = _jwtHelper.GenerateToken(user.userId, user.username),
                userId = user.userId,
                displayName = user.displayName,
                expiresAt = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds()
            };
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            return Task.FromResult(_jwtHelper.ValidateToken(token));
        }

        private static string HashPassword(string password)
        {
            // TODO: Sử dụng BCrypt hoặc Argon2 cho production
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private static bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}
