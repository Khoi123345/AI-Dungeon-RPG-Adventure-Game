using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// API service cho Battle feature.
/// POST /battle/spawn-boss và POST /battle/resolve
/// </summary>
public class BattleApiService
{
    public async Task<string> SpawnBossAsync(string characterId, string sessionId)
    {
        var body = JsonUtility.ToJson(new SpawnBody { characterId = characterId, sessionId = sessionId });
        return await ApiClient.Instance.PostRawAsync("/battle/spawn-boss", body);
    }

    public async Task<string> ResolveBattleAsync(string characterId, string encounterId)
    {
        var body = JsonUtility.ToJson(new ResolveBody { characterId = characterId, encounterId = encounterId });
        return await ApiClient.Instance.PostRawAsync("/battle/resolve", body);
    }

    [System.Serializable]
    private class SpawnBody { public string characterId; public string sessionId; }

    [System.Serializable]
    private class ResolveBody { public string characterId; public string encounterId; }
}
