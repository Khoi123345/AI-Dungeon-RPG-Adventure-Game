using UnityEngine;

[System.Serializable]
public class ShopItemData
{
    public string itemName;
    [TextArea] public string itemDescription;
    public int price;
    public Sprite icon;
}