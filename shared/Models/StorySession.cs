using System;

namespace GameShared.Models
{
    [Serializable]
    public class StorySession
    {
        public string sessionId { get; set; }
        public string characterId { get; set; }
        public string currentLocation { get; set; }
        public string currentNodeId { get; set; }
        public string currentChapterId { get; set; }
        public string status { get; set; }
        public DateTime updatedAt { get; set; }
        public DateTime? endedAt { get; set; }
        public string storyVersion { get; set; }
        public string storySummary { get; set; }
        public string sourceType { get; set; }
    }
}
