using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GameBackend.Core.Utils
{
    /// <summary>
    /// JWT Helper — Plan A (Self-managed auth).
    /// Tạo và validate JWT token dùng HS256 với secret key từ environment variable.
    /// Plan B (Cognito): Class này không được dùng; token validate tự động bởi API Gateway Cognito Authorizer.
    /// </summary>
    public class JwtHelper
    {
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly SymmetricSecurityKey _signingKey;
        private readonly JwtSecurityTokenHandler _handler;

        public JwtHelper()
        {
            _secretKey  = Environment.GetEnvironmentVariable("JWT_SECRET")   ?? "dev-secret-key-must-be-at-least-32-chars!!";
            _issuer     = Environment.GetEnvironmentVariable("JWT_ISSUER")   ?? "ai-dungeon-rpg";
            _audience   = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "ai-dungeon-rpg-client";
            _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            _handler    = new JwtSecurityTokenHandler();
        }

        /// <summary>
        /// Tạo JWT token chuẩn HS256.
        /// Claims: sub (userId), username, iss, aud, exp, iat.
        /// </summary>
        public string GenerateToken(string userId, string username, int expiryHours = 24)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,  userId),
                new Claim(JwtRegisteredClaimNames.Jti,  Guid.NewGuid().ToString()),
                new Claim("username", username),
            };

            var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer:             _issuer,
                audience:           _audience,
                claims:             claims,
                notBefore:          DateTime.UtcNow,
                expires:            DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: credentials
            );

            return _handler.WriteToken(token);
        }

        /// <summary>
        /// Validate JWT token — kiểm tra signature, issuer, audience, expiry.
        /// Trả về ClaimsPrincipal nếu hợp lệ, null nếu không hợp lệ.
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var parameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = _signingKey,
                    ValidateIssuer           = true,
                    ValidIssuer              = _issuer,
                    ValidateAudience         = true,
                    ValidAudience            = _audience,
                    ValidateLifetime         = true,
                    ClockSkew                = TimeSpan.FromMinutes(5)
                };

                return _handler.ValidateToken(token, parameters, out _);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Kiểm tra token có hợp lệ không (bool overload cho IAuthService).</summary>
        public bool IsTokenValid(string token) => ValidateToken(token) != null;

        /// <summary>Lấy userId từ token (sub claim).</summary>
        public string? GetUserIdFromToken(string token)
        {
            var principal = ValidateToken(token);
            return principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        }
    }
}

