using System;

namespace GameShared.Models
{
    /// <summary>
    /// Nhân vật chơi. Chuẩn hóa naming convention sang camelCase.
    /// </summary>
    [Serializable]
    public class Character
    {
        public string characterId { get; set; }
        public string userId { get; set; }
        public string name { get; set; }
        public int level { get; set; }
        public int experience { get; set; }
        public int hp { get; set; }
        public int maxHp { get; set; }
        public int mp { get; set; }
        public int maxMp { get; set; }
        public int attack { get; set; }
        public int defense { get; set; }
        public float criticalRate { get; set; }
        public float luckyRate { get; set; }
        public int gold { get; set; }
        public string className { get; set; }
        public string status { get; set; }
        public string currentLocationId { get; set; }
        public DateTime reviveTime { get; set; }
    }
}
