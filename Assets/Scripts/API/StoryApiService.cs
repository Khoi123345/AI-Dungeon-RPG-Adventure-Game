using System.Threading.Tasks;
using UnityEngine;
using GameShared.DTOs.Story;

/// <summary>
/// API service cho Story feature.
/// POST story/start và POST story/action
/// </summary>
public class StoryApiService
{
    public async Task<StoryActionResponse> StartStoryAsync(string characterId, string storyFileId = "prologue")
    {
        bool forceNew = GameProgressService.Instance != null && GameProgressService.Instance.ShouldForceNewSession;
        if (forceNew && GameProgressService.Instance != null)
        {
            GameProgressService.Instance.ShouldForceNewSession = false; // Reset flag sau khi dùng
        }

        var body = new StoryStartBody { characterId = characterId, storyFileId = storyFileId, forceNewSession = forceNew };
        return await ApiClient.Instance.PostAsync<StoryActionResponse>("story/start", body);
    }

    public async Task<StoryActionResponse> SendActionAsync(string characterId, string sessionId, int choiceIndex, string playerInput)
    {
        var body = new StoryActionBody
        {
            characterId = characterId,
            sessionId = sessionId,
            choiceIndex = choiceIndex,
            playerInput = playerInput
        };
        return await ApiClient.Instance.PostAsync<StoryActionResponse>("story/action", body);
    }

    [System.Serializable]
    private class StoryStartBody { public string characterId; public string storyFileId; public bool forceNewSession; }

    [System.Serializable]
    private class StoryActionBody { public string characterId; public string sessionId; public int choiceIndex; public string playerInput; }
}
