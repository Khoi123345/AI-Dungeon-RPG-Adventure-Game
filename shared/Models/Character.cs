using System;

namespace GameShared.Models
{
    /// <summary>
    /// Nhân vật chơi. Thêm mp/maxMp (đã có trong GameProgressService).
    /// Chuẩn hóa naming convention sang camelCase.
    /// </summary>
    [Serializable]
    public class Character
    {
        public string characterId;
        public string userId;
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
        public DateTime reviveTime;
    }
}
