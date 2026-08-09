using System;

namespace GameShared.DTOs.Story
{
    [Serializable]
    public class StoryStartRequest
    {
        public string characterId;
        public string storyFileId;
        /// <summary>
        /// Nếu true: xóa session cũ và bắt đầu lại từ đầu (khi người chơi chết và chọn Back to Menu).
        /// </summary>
        public bool forceNewSession;
    }
}
