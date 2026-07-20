using System.Text;
using GameShared.Models;
using GameBackend.Core.AIStory.Formatters.Interfaces;

namespace GameBackend.Core.AIStory.Formatters;

public class InventoryFormatter : IInventoryFormatter
{
    public string Format(IEnumerable<Item> items)
    {
        var sb = new StringBuilder();

        foreach (var item in items)
        {
            sb.AppendLine(
                $"- {item.name} ({item.rarity}");
        }

        return sb.ToString();
    }
}