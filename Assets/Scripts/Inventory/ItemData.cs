using UnityEngine;

// Định nghĩa các loại vật phẩm có trong game
public enum ItemType
{
    Weapon,      // Vũ khí
    Armor,       // Giáp
    Accessory,   // Phụ kiện
    Consumable   // Vật phẩm tiêu hao
}

// Định nghĩa độ hiếm của vật phẩm
public enum ItemRarity
{
    Common,      // Thường (Màu xám)
    Rare,        // Hiếm (Màu xanh dương)
    Epic,        // Sử thi (Màu tím)
    Legendary    // Huyền thoại (Màu vàng/cam)
}

// Đổi thành [System.Serializable] để có thể dùng "new ItemData()" trong code
// Không còn là MonoBehaviour — không cần attach vào GameObject nữa
[System.Serializable]
public class ItemData
{
    public string itemName;
    public Sprite itemIcon;
    public ItemType itemType;
    public ItemRarity itemRarity; // Độ hiếm của vật phẩm

    // Thêm chỉ số chiến đấu
    public int atkBonus;
    public int defBonus;
    public int quantity = 1; // Số lượng mặc định là 1
    public string inventoryId;
    public bool isEquipped;

    public static ItemType GetItemTypeFromId(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return ItemType.Weapon;
        string lower = itemId.ToLower();
        if (lower.Contains("ring") || lower.Contains("amulet") || lower.Contains("necklace") || lower.Contains("accessory") || lower.Contains("void"))
            return ItemType.Accessory;
        if (lower.Contains("armor") || lower.Contains("vest") || lower.Contains("shield") || lower.Contains("plate") || lower.Contains("helmet") || lower.Contains("boots"))
            return ItemType.Armor;
        if (lower.Contains("potion") || lower.Contains("elixir") || lower.Contains("consumable"))
            return ItemType.Consumable;
        return ItemType.Weapon;
    }
}
