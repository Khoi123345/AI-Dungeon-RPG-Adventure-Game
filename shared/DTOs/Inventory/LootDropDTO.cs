using System;
using System.Collections.Generic;

namespace GameShared.DTOs.Inventory
{
    [Serializable]
    public class LootDropDTO
    {
        public string playerId;          // character_id trong CSDL (Khóa ngoại của bảng Character / Inventory)
        public string battleId;          // battle_id trong CSDL (Khóa ngoại của bảng Battle / LootDrop)
        public List<LootItemDTO> items;  // Danh sách các vật phẩm rơi kèm số lượng (bảng LootDrop & Inventory)
    }

    [Serializable]
    public class LootItemDTO
    {
        public string itemId;            // item_id trong CSDL
        public int quantity;             // quantity trong CSDL
    }
}
