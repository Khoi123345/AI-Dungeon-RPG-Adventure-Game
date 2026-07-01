using System.Text;
using GameShared.Models;
using GameBackend.Core.AIStory.Formatters.Interfaces;

namespace GameBackend.Core.AIStory.Formatters;

public class RecentTurnsFormatter
    : IRecentTurnsFormatter
{
    public string Format(
        IEnumerable<StoryAction> actions)
    {
        var sb = new StringBuilder();

        foreach (var action in actions)
        {
            sb.AppendLine(
                $"User: {action.playerInput}");

            sb.AppendLine(
                $"AI: {action.aiResponse}");

            sb.AppendLine();
        }

        return sb.ToString();
    }
}