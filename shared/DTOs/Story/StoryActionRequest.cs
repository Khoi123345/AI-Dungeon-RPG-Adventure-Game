using System;

namespace GameShared.DTOs.Story
{
    [Serializable]
    public class StoryActionRequest
    {
        public string characterId;
        public string sessionId;
        public int choiceIndex;
        public string playerInput;
    }
}
