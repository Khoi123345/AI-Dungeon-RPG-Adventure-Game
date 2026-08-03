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
}
