using GameShared.DTOs.Auth;

namespace GameBackend.Core.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> RegisterAsync(string username, string email, string password);
        Task<bool> ValidateTokenAsync(string token);
    }
}
