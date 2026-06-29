using System;

namespace GameShared.Models
{
    [Serializable]
    public class StorySession
    {
        public string sessionId;
        public string characterId;
        public string currentLocation;
        public string currentNodeId;
        public string currentChapterId;
        public string storyContext;
        public string status;
        public DateTime updatedAt;
        public DateTime? endedAt;
        public string storyVersion;
        public string storySummary;
        public string sourceType;
    }
}
