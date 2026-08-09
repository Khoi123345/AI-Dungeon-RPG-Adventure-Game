using System;
using System.Collections.Generic;
using GameShared.DTOs.Character;

namespace GameShared.DTOs.Story
{
    [Serializable]
    public class StoryActionResponse
    {
        public string sessionId;
        public string currentNodeId;
        public string currentLocation;
        public string narrativeText;
        public List<StoryChoiceOption> choices;
        public CharacterResponse character;
        public bool triggerBattle;
        public string bossId;
        public string? bossName;
        public int? bossLevel;
        public string? debugPrompt;
    }


    [Serializable]
    public class StoryChoiceOption
    {
        public string label;
        public string description;
        public string nextNodeId;
    }
}
