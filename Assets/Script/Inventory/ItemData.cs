using UnityEngine;

// Định nghĩa các loại vật phẩm có trong game
public enum ItemType
{
    Weapon,      // Vũ khí
    Armor,       // Giáp
    Accessory,   // Phụ kiện
    Consumable   // Vật phẩm tiêu hao
}

public class ItemData : MonoBehaviour
{
    public string itemName;
    public Sprite itemIcon;
    public ItemType itemType; 
}