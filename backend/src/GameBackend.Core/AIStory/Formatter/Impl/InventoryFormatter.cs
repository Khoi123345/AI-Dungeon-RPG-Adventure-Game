using System.Text;
using GameShared.Models;
using GameBackend.Core.AIStory.Formatters.Interfaces;

namespace GameBackend.Core.AIStory.Formatters;

public class InventoryFormatter : IInventoryFormatter
{
    public string Format(IEnumerable<Item> items)
    {
        var itemList = items?.ToList() ?? new List<Item>();
        if (itemList.Count == 0)
        {
            return "Túi đồ rỗng (Không có vật phẩm nào).";
        }

        var sb = new StringBuilder();
        foreach (var item in itemList)
        {
            var desc = string.IsNullOrWhiteSpace(item.description) ? "" : $" - {item.description}";
            sb.AppendLine($"- ID: {item.itemId} | Tên: {item.name} | Loại: {item.itemType ?? item.rarity}{desc}");
        }

        return sb.ToString();
    }
}