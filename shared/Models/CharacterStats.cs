using System;

namespace GameShared.Models
{
    /// <summary>
    /// Chỉ số thực tế (effective stats) của nhân vật sau khi cộng bonus trang bị.
    /// Dùng trong BattleService để tính Player Power và trong CharacterResponse để hiển thị.
    /// </summary>
    [Serializable]
    public class CharacterStats
    {
        /// <summary>HP tối đa thực tế = Character.maxHp + Σ(equip.hpBonus)</summary>
        public int maxHp;

        /// <summary>Tấn công thực tế = Character.attack + Σ(equip.attackBonus)</summary>
        public int attack;

        /// <summary>Phòng thủ thực tế = Character.defense + Σ(equip.defenseBonus)</summary>
        public int defense;

        /// <summary>Tỷ lệ chí mạng thực tế = Character.criticalRate + Σ(equip.criticalBonus)</summary>
        public float criticalRate;

        /// <summary>Tỷ lệ may mắn (dùng cho Lucky Factor trong battle) — lấy từ base, không có bonus trang bị.</summary>
        public float luckyRate;
    }
}
