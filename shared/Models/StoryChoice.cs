using System;

namespace GameShared.Models
{
    [Serializable]
    public class StoryChoice
    {
        public string label;
        public string description;
        public string nextNodeId;
        public int goldDelta;
        public int hpDelta;
        public int expDelta;
    }
}
