using System;

namespace GameShared.DTOs.Character
{
    [Serializable]
    public class CharacterResponse
    {
        public string characterId;
        public string name;
        public int level;
        public int experience;
        public int hp;
        public int maxHp;
        public int mp;
        public int maxMp;
        public int attack;
        public int defense;
        public float criticalRate;
        public float luckyRate;
        public int gold;
        public string className;
        public string status;
        public string currentLocationId;
    }
}
