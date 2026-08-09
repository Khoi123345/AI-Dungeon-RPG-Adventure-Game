using System;
using System.Collections.Generic;
using System.Text;
using GameShared.Models;
using GameBackend.Core.AIStory.Formatters.Interfaces;

namespace GameBackend.Core.AIStory.Formatters
{
    public class RecentTurnsFormatter : IRecentTurnsFormatter
    {
        public string Format(IEnumerable<StoryAction> actions)
        {
            var sb = new StringBuilder();

            foreach (var action in actions)
            {
                if (string.Equals(action.actionType, "battle_result", StringComparison.OrdinalIgnoreCase))
                {
                    sb.AppendLine($"[TRẬN ĐÁNH VỪA KẾT THÚC] Hành động: {action.playerInput}");
                    sb.AppendLine($"[KẾT QUẢ VÀ BÀN GIAO] AI: {action.aiResponse}");
                }
                else
                {
                    sb.AppendLine($"User: {action.playerInput}");
                    sb.AppendLine($"AI: {action.aiResponse}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}