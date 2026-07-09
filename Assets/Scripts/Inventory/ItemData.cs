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
}