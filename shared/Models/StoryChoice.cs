using System;

namespace GameShared.Models
{
    [Serializable]
    public class StoryChoice
    {
        public string label { get; set; }
        public string description { get; set; }
        public string nextNodeId { get; set; }
        public int goldDelta { get; set; }
        public int hpDelta { get; set; }
        public int expDelta { get; set; }
    }
}
