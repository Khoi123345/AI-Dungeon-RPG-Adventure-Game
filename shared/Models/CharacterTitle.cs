using System;

namespace GameShared.Models
{
    /// <summary>
    /// Danh hiệu (Title/Achievement) gắn với nhân vật.
    /// Hiển thị trong tab "Danh Hiệu" của ProfileScene.
    /// </summary>
    [Serializable]
    public class CharacterTitle
    {
        public string titleId;
        public string characterId;

        /// <summary>Tên danh hiệu hiển thị, VD: "Kẻ Tiêu Diệt Bóng Tối"</summary>
        public string name;

        /// <summary>Mô tả điều kiện đạt được, VD: "Hạ gục Shadow Demon lần đầu tiên"</summary>
        public string description;

        /// <summary>Common | Rare | Epic | Legendary</summary>
        public string rarity;

        /// <summary>Đang trang bị (hiển thị trên đầu tên nhân vật) hay không</summary>
        public bool isEquipped;

        /// <summary>Thời điểm nhận được danh hiệu</summary>
        public DateTime earnedAt;
    }
}
