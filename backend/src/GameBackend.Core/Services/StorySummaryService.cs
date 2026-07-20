using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameBackend.Core.Services.Interfaces;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services
{
    public class StorySummaryService : IStorySummaryService
    {
        private const int TurnInterval = 20;
        private const string SystemPrompt = "Bạn là trợ lý Dungeon Master chuyên tóm tắt cốt truyện game RPG dark fantasy. Nhiệm vụ của bạn là đọc toàn bộ diễn biến cốt truyện hiện tại cùng các hành động gần nhất, sau đó viết lại thành một bản tóm tắt cốt truyện duy nhất bằng Tiếng Việt (khoảng 300 từ). Bản tóm tắt phải cô đọng, giữ lại các sự kiện chính, quyết định quan trọng của người chơi, vị trí hiện tại và mục tiêu tiếp theo. Không thêm lời chào hay giải thích ngoài lề.";

        private readonly IBedrockService _bedrockService;
        private readonly ILogger<StorySummaryService> _logger;

        public StorySummaryService(
            IBedrockService bedrockService,
            ILogger<StorySummaryService> logger)
        {
            _bedrockService = bedrockService;
            _logger = logger;
        }

        public async Task<string> CondenseSummaryIfNeededAsync(StorySession session, int currentTurnCount, List<StoryAction> recentActions)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            // Chỉ kích hoạt tóm tắt định kỳ mỗi 20 turn
            if (currentTurnCount <= 0 || currentTurnCount % TurnInterval != 0)
            {
                return session.storySummary ?? string.Empty;
            }

            _logger.LogInformation("Triggering story summary condensation at turn {TurnCount} for session {SessionId}", currentTurnCount, session.sessionId);

            try
            {
                if (!await _bedrockService.IsAvailableAsync())
                {
                    _logger.LogWarning("Bedrock service is unavailable. Skipping summary condensation for turn {TurnCount}", currentTurnCount);
                    return session.storySummary ?? string.Empty;
                }

                var sb = new StringBuilder();
                sb.AppendLine($"Tóm tắt cốt truyện trước đó:\n{session.storySummary ?? "Chưa có tóm tắt."}\n");
                sb.AppendLine("Diễn biến các hành động gần đây:");

                if (recentActions != null && recentActions.Count > 0)
                {
                    var sortedActions = recentActions.OrderBy(a => a.turnNumber > 0 ? a.turnNumber : a.createdAt.Ticks).ToList();
                    foreach (var act in sortedActions)
                    {
                        var turnLabel = act.turnNumber > 0 ? $"Turn {act.turnNumber}" : "Lượt";
                        sb.AppendLine($"- [{turnLabel}] Người chơi: {act.playerInput}");
                        if (!string.IsNullOrWhiteSpace(act.aiResponse))
                        {
                            var shortAi = act.aiResponse.Length > 150 ? act.aiResponse[..150] + "..." : act.aiResponse;
                            sb.AppendLine($"  Kết quả: {shortAi}");
                        }
                    }
                }

                sb.AppendLine("\nYêu cầu: Viết lại bản tóm tắt cốt truyện mới duy nhất (khoảng 300 từ) cô đọng lại toàn bộ hành trình trên.");

                var userPrompt = sb.ToString();
                var newSummary = await _bedrockService.GenerateNarrativeAsync(SystemPrompt, userPrompt);

                if (!string.IsNullOrWhiteSpace(newSummary))
                {
                    newSummary = newSummary.Trim();
                    _logger.LogInformation("Successfully condensed story summary for session {SessionId} at turn {TurnCount}. New length: {Length} chars",
                        session.sessionId, currentTurnCount, newSummary.Length);

                    session.storySummary = newSummary;
                    return newSummary;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to condense story summary at turn {TurnCount} for session {SessionId}", currentTurnCount, session.sessionId);
            }

            return session.storySummary ?? string.Empty;
        }
    }
}
