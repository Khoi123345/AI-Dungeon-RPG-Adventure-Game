using System;
using System.Collections.Generic;

[Serializable]
public class StoryCharacterState
{
    public string characterName;
    public int level;
    public int hp;
    public int gold;
}

[Serializable]
public class StoryChoiceData
{
    public string label;
    public string description;
    public string nextNodeId;
}

[Serializable]
public class StoryLineData
{
    public string text;
    public float pauseAfter;
}

[Serializable]
public class StoryNodeData
{
    public string nodeId;
    public string backgroundKey;
    public StoryCharacterState character;
    public List<StoryLineData> lines = new List<StoryLineData>();
    public List<StoryChoiceData> choices = new List<StoryChoiceData>();
}

[Serializable]
public class StoryData
{
    public string title;
    public StoryNodeData node;
}
