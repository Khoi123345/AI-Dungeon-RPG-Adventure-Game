using System;

namespace GameShared.DTOs.Story
{
    [Serializable]
    public class StoryStartRequest
    {
        public string characterId;
        public string storyFileId;
    }
}
