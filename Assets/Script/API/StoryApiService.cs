using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// API service cho Story feature.
/// POST /story/start và POST /story/action
/// </summary>
public class StoryApiService
{
    public async Task<string> StartStoryAsync(string characterId, string storyFileId)
    {
        var body = JsonUtility.ToJson(new StoryStartBody { characterId = characterId, storyFileId = storyFileId });
        return await ApiClient.Instance.PostRawAsync("/story/start", body);
    }

    public async Task<string> SendActionAsync(string characterId, string sessionId, int choiceIndex, string playerInput)
    {
        var body = JsonUtility.ToJson(new StoryActionBody
        {
            characterId = characterId,
            sessionId = sessionId,
            choiceIndex = choiceIndex,
            playerInput = playerInput
        });
        return await ApiClient.Instance.PostRawAsync("/story/action", body);
    }

    [System.Serializable]
    private class StoryStartBody { public string characterId; public string storyFileId; }

    [System.Serializable]
    private class StoryActionBody { public string characterId; public string sessionId; public int choiceIndex; public string playerInput; }
}
