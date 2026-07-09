using System.Text.Json;

namespace GameBackend.Core.Utils
{
    /// <summary>
    /// Utility class for System.Text.Json serialization and deserialization.
    /// Configured to include fields (required for Unity JsonUtility compatibility)
    /// and case-insensitivity.
    /// </summary>
    public static class JsonUtils
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = true
        };

        public static T? Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            return JsonSerializer.Deserialize<T>(json, Options);
        }

        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, Options);
        }
    }
}
