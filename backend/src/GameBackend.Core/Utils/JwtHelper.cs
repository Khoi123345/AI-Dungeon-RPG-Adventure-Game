namespace GameBackend.Core.Utils
{
    public class JwtHelper
    {
        private readonly string _secretKey;

        public JwtHelper()
        {
            _secretKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "dev-secret-key-change-in-production";
        }

        /// <summary>
        /// Tạo JWT token đơn giản.
        /// TODO: Thay bằng thư viện System.IdentityModel.Tokens.Jwt cho production.
        /// </summary>
        public string GenerateToken(string userId, string username)
        {
            string header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"HS256\",\"typ\":\"JWT\"}"));
            string payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(
                $"{{\"userId\":\"{userId}\",\"username\":\"{username}\",\"exp\":{DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds()}}}"));

            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(_secretKey));
            string signature = Convert.ToBase64String(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes($"{header}.{payload}")));

            return $"{header}.{payload}.{signature}";
        }

        public bool ValidateToken(string token)
        {
            // TODO: Implement proper JWT validation cho production
            return !string.IsNullOrWhiteSpace(token) && token.Split('.').Length == 3;
        }
    }
}
