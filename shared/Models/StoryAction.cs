using System;

namespace GameShared.Models
{
    [Serializable]
    public class StoryAction
    {
        public string actionId { get; set; }
        public string sessionId { get; set; }
        public string playerInput { get; set; }
        public string aiResponse { get; set; }
        public int turnNumber { get; set; }
        public string actionType { get; set; }
        public string metadataJson { get; set; }
        public DateTime createdAt { get; set; }
    }
}
