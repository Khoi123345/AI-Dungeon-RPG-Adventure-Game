using System;

namespace GameShared.Models
{
    [Serializable]
    public class StoryAction
    {
        public string actionId;
        public string sessionId;
        public string playerInput;
        public string aiResponse;
        public int turnNumber;
        public string actionType;
        public string metadataJson;
        public DateTime createdAt;
    }
}
