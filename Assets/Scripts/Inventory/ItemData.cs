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
    Epic         // Sử thi (Màu tím)
}

public class ItemData : MonoBehaviour
{
    public string itemName;
    public Sprite itemIcon;
    public ItemType itemType; 
    public ItemRarity itemRarity; // Độ hiếm của vật phẩm

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