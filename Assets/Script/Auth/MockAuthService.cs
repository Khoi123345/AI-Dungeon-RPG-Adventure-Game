using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GameShared.Models;
using UnityEngine;

/// <summary>
/// Mock Auth Service — Plan A (Test trên máy, không cần AWS).
/// Lưu users vào PlayerPrefs["mock_users"] dạng JSON.
/// Simulate network delay 400ms để giống thực tế.
/// Có sẵn tài khoản demo: username="demo" password="password123".
/// </summary>
public class MockAuthService : IUnityAuthService
{
    // ── PlayerPrefs Keys ──────────────────────────────────────────
    private const string KEY_MOCK_USERS    = "mock_auth_users";
    private const string KEY_AUTH_TOKEN    = "mock_auth_token";
    private const string KEY_AUTH_USER_ID  = "mock_auth_userId";
    private const string KEY_AUTH_DISPLAY  = "mock_auth_displayName";
    private const string KEY_AUTH_EXPIRES  = "mock_auth_expiresAt";

    private const int    MOCK_DELAY_MS     = 400;
    private const int    TOKEN_HOURS       = 24;

    // ── State ─────────────────────────────────────────────────────
    private string _currentToken;
    private User   _currentUser;

    public bool   IsLoggedIn    => !string.IsNullOrEmpty(_currentToken);
    public string CurrentToken  => _currentToken;
    public User   CurrentUser   => _currentUser;

    // ── Constructor ───────────────────────────────────────────────
    public MockAuthService()
    {
        EnsureDemoUserExists();
    }

    // ══════════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════════

    public async Task<AuthResult> LoginAsync(string username, string password)
    {
        await SimulateDelay();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("INVALID_REQUEST", "Username và password không được để trống.");

        var users = LoadUsers();
        var stored = users.Find(u => string.Equals(u.username, username.Trim(), StringComparison.OrdinalIgnoreCase));

        if (stored == null)
            return AuthResult.Fail("INVALID_CREDENTIALS", "Username không tồn tại.");

        if (!VerifyPassword(password, stored.passwordHash))
            return AuthResult.Fail("INVALID_CREDENTIALS", "Mật khẩu không đúng.");

        if (stored.status != "Active")
            return AuthResult.Fail("ACCOUNT_DISABLED", "Tài khoản đã bị vô hiệu hóa.");

        // Update lastLogin
        stored.lastLoginAt = DateTime.UtcNow.Ticks;
        SaveUsers(users);

        string token     = GenerateMockToken(stored.userId, stored.username);
        long   expiresAt = DateTimeOffset.UtcNow.AddHours(TOKEN_HOURS).ToUnixTimeSeconds();

        SetSession(token, ToUser(stored), expiresAt);
        Debug.Log($"[MockAuth] Login success: {username}");
        return AuthResult.Ok(token, stored.userId, stored.displayName, expiresAt);
    }

    public async Task<AuthResult> RegisterAsync(string username, string email, string password, string confirmPassword)
    {
        await SimulateDelay();

        // Validate
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail("INVALID_REQUEST", "Vui lòng điền đầy đủ thông tin.");

        if (password != confirmPassword)
            return AuthResult.Fail("PASSWORD_MISMATCH", "Mật khẩu xác nhận không khớp.");

        if (password.Length < 6)
            return AuthResult.Fail("WEAK_PASSWORD", "Mật khẩu phải có ít nhất 6 ký tự.");

        var users = LoadUsers();

        if (users.Exists(u => string.Equals(u.username, username.Trim(), StringComparison.OrdinalIgnoreCase)))
            return AuthResult.Fail("USERNAME_EXISTS", $"Username '{username}' đã được sử dụng.");

        if (users.Exists(u => string.Equals(u.email, email.Trim(), StringComparison.OrdinalIgnoreCase)))
            return AuthResult.Fail("EMAIL_EXISTS", $"Email '{email}' đã được đăng ký.");

        // Create new user
        var newUser = new User
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

        users.Add(ToRecord(newUser));
        SaveUsers(users);

        string token     = GenerateMockToken(newUser.userId, newUser.username);
        long   expiresAt = DateTimeOffset.UtcNow.AddHours(TOKEN_HOURS).ToUnixTimeSeconds();

        SetSession(token, newUser, expiresAt);
        Debug.Log($"[MockAuth] Register success: {username}");
        return AuthResult.Ok(token, newUser.userId, newUser.displayName, expiresAt);
    }

    /// <summary>Plan A không cần OTP confirm — luôn trả về success.</summary>
    public Task<AuthResult> ConfirmSignUpAsync(string username, string confirmationCode)
    {
        return Task.FromResult(new AuthResult { success = true });
    }

    public Task LogoutAsync()
    {
        _currentToken = null;
        _currentUser  = null;
        ApiClient.Instance.ClearAuth();
        Debug.Log("[MockAuth] Logged out.");
        return Task.CompletedTask;
    }

    public async Task<bool> TryRestoreSessionAsync()
    {
        await Task.Yield();
        // Không tự động phục hồi phiên đăng nhập khi chạy thử tạm thời
        return false;
    }

    // ══════════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ══════════════════════════════════════════════════════════════

    private void SetSession(string token, User user, long expiresAt)
    {
        _currentToken = token;
        _currentUser  = user;

        // Chỉ lưu trong bộ nhớ tạm (runtime), không ghi xuống PlayerPrefs
        ApiClient.Instance.SetAuth(token);
    }

    private static async Task SimulateDelay()
    {
        // Mô phỏng network latency cho giống thực tế
        int elapsed = 0;
        while (elapsed < MOCK_DELAY_MS)
        {
            await Task.Yield();
            elapsed += 16; // ~60fps
        }
    }

    // ── Mock User Storage (In-Memory Static List) ─────────────────
    private static List<MockUserRecord> _inMemoryUsers;

    private List<MockUserRecord> LoadUsers()
    {
        if (_inMemoryUsers == null)
        {
            _inMemoryUsers = new List<MockUserRecord>();
        }
        return _inMemoryUsers;
    }

    private void SaveUsers(List<MockUserRecord> users)
    {
        _inMemoryUsers = users;
    }

    private void EnsureDemoUserExists()
    {
        var users = LoadUsers();
        if (users.Exists(u => u.username == "demo")) return;

        users.Add(new MockUserRecord
        {
            userId       = "demo-user-001",
            username     = "demo",
            email        = "demo@example.com",
            passwordHash = HashPassword("password123"),
            displayName  = "Demo Player",
            status       = "Active",
            createdAt    = DateTime.UtcNow.AddDays(-7).Ticks,
            lastLoginAt  = DateTime.UtcNow.Ticks
        });

        SaveUsers(users);
        Debug.Log("[MockAuth] Demo user created in-memory: username=demo, password=password123");
    }

    // ── Token ─────────────────────────────────────────────────────

    /// <summary>Tạo fake JWT-like token. Không dùng cho production.</summary>
    private static string GenerateMockToken(string userId, string username)
    {
        string header  = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"alg\":\"MOCK\",\"typ\":\"JWT\"}"));
        string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $"{{\"userId\":\"{userId}\",\"username\":\"{username}\",\"exp\":{DateTimeOffset.UtcNow.AddHours(TOKEN_HOURS).ToUnixTimeSeconds()}}}"));
        return $"{header}.{payload}.mock_signature";
    }

    // ── Password ──────────────────────────────────────────────────

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes("mock_salt_" + password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash) => HashPassword(password) == hash;

    // ── Serializable Types ────────────────────────────────────────

    [Serializable]
    private class MockUserRecord
    {
        public string userId;
        public string username;
        public string email;
        public string passwordHash;
        public string displayName;
        public string status;
        public long   createdAt;
        public long   lastLoginAt;
    }

    [Serializable]
    private class MockUserListWrapper
    {
        public List<MockUserRecord> users = new List<MockUserRecord>();
    }

    private static MockUserRecord ToRecord(User u) => new MockUserRecord
    {
        userId       = u.userId,
        username     = u.username,
        email        = u.email,
        passwordHash = u.passwordHash,
        displayName  = u.displayName,
        status       = u.status,
        createdAt    = u.createdAt.Ticks,
        lastLoginAt  = u.lastLoginAt.Ticks
    };

    // Helper: Convert MockUserRecord → GameShared.Models.User
    private static List<MockUserRecord> ToRecords(List<User> users)
    {
        var list = new List<MockUserRecord>();
        foreach (var u in users)
            list.Add(ToRecord(u));
        return list;
    }

    private static User ToUser(MockUserRecord r) => new User
    {
        userId       = r.userId,
        username     = r.username,
        email        = r.email,
        passwordHash = r.passwordHash,
        displayName  = r.displayName,
        status       = r.status,
        createdAt    = new DateTime(r.createdAt, DateTimeKind.Utc),
        lastLoginAt  = new DateTime(r.lastLoginAt, DateTimeKind.Utc)
    };

    // Override LoadUsers/SaveUsers to work with User model properly
    private List<User> LoadUsersAsModel()
    {
        var records = LoadUsers();
        var users   = new List<User>();
        foreach (var r in records) users.Add(ToUser(r));
        return users;
    }
}
