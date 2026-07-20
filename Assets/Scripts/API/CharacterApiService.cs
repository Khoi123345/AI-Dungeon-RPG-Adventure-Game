using System.Threading.Tasks;

/// <summary>
/// API service cho Character feature.
/// GET /character/{id} và POST /character
/// </summary>
public class CharacterApiService
{
    public async Task<string> GetCharacterAsync(string characterId)
    {
        return await ApiClient.Instance.GetRawAsync("/character/" + characterId);
    }

    public async Task<string> CreateCharacterAsync(string userId, string name, string className)
    {
        var body = UnityEngine.JsonUtility.ToJson(new CreateBody { userId = userId, name = name, className = className });
        return await ApiClient.Instance.PostRawAsync("/character", body);
    }

    [System.Serializable]
    private class CreateBody { public string userId; public string name; public string className; }
}
