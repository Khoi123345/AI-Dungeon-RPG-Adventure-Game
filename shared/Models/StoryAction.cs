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
        /// <summary>Index lựa chọn người chơi đã chọn (dùng bởi Unity GameProgressService).</summary>
        public int choiceIndex;
        public string metadataJson;
        public DateTime createdAt;
    }
}
