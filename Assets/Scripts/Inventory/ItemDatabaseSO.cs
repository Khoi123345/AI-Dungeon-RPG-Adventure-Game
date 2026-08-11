using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabaseSO : ScriptableObject
{
    [Header("Danh sách toàn bộ vật phẩm trong Game")]
    public List<ItemData> items = new List<ItemData>();

    /// <summary>
    /// Tìm một vật phẩm theo tên (hoặc ID). Bỏ qua viết hoa viết thường.
    /// </summary>
    public ItemData FindItemByNameOrId(string nameOrId)
    {
        if (string.IsNullOrEmpty(nameOrId)) return null;

        return items.Find(x => x != null && 
            !string.IsNullOrEmpty(x.itemId) &&
            x.itemId.Equals(nameOrId, System.StringComparison.OrdinalIgnoreCase));
    }
}
