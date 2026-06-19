using System;

namespace GameShared.Models
{
    [Serializable]
    public class StorySession
    {
        public string sessionId;
        public string characterId;
        public string currentLocation;
        public string currentChapterId;
        public string status;
        public DateTime updatedAt;
        public DateTime? endedAt;
        public string storyVersion;
        public string storySummary;
        public string sourceType;
    }
}
