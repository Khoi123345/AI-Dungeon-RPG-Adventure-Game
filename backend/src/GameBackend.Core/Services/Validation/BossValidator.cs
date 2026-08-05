using GameBackend.Core.AIStory.Services;
using GameShared.DTOs.Story;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services.Validation
{
    public sealed class BossValidator : IGameRuleSubValidator
    {
        private readonly IContentService _contentService;
        private readonly ILogger<BossValidator> _logger;

        private static readonly string[] BattleKeywords = new[]
        {
            "trận chiến", "giao chiến", "tấn công", "chiến đấu", "khiêu chiến",
            "bắt đầu trận", "quái vật", "sói bóng tối", "gặp boss", "vua goblin",
            "đánh boss", "đánh quái", "vào trận", "thách đấu"
        };

        public BossValidator(IContentService contentService, ILogger<BossValidator> logger)
        {
            _contentService = contentService;
            _logger = logger;
        }

        public async Task ValidateAsync(GameRuleValidationContext context)
        {
            var response = context.Response;

            // Tự động phát hiện ý định chiến đấu từ văn bản câu chuyện của AI Nova Pro
            bool narrativeIntendsBattle = !string.IsNullOrWhiteSpace(response.NarrativeText) && ContainsBattleKeyword(response.NarrativeText);

            if (narrativeIntendsBattle)
            {
                response.TriggerBattle = true;
            }

            if (!response.TriggerBattle)
            {
                ResetBossFields(response);
                return;
            }

            // Tự động nhận diện chính xác BossId dựa theo tên Boss xuất hiện trong văn bản
            var lowerText = (response.NarrativeText ?? string.Empty).ToLowerInvariant();
            if (lowerText.Contains("goblin"))
            {
                response.BossId = "goblin_king";
                response.BossName = "Vua Goblin";
            }
            else if (lowerText.Contains("demon") || lowerText.Contains("ác demon"))
            {
                response.BossId = "shadow_demon";
                response.BossName = "Ác Demon Bóng Tối";
            }
            else if (lowerText.Contains("dragon") || lowerText.Contains("rồng"))
            {
                response.BossId = "dragon_king";
                response.BossName = "Hỏa Long Vương";
            }

            // Nếu BossId vẫn rỗng hoặc không tồn tại trong hệ thống, tự động gán fallback Boss "goblin_king"
            if (string.IsNullOrWhiteSpace(response.BossId) || !await _contentService.BossExistsAsync(response.BossId))
            {
                _logger.LogInformation("BossId '{BossId}' invalid or missing, falling back to 'goblin_king'", response.BossId);
                response.BossId = "goblin_king";
                response.BossName = "Vua Goblin";
            }

            response.BossName = string.IsNullOrWhiteSpace(response.BossName) ? response.BossId : response.BossName;
            var suggestedLevel = response.BossLevel ?? 1;
            response.BossLevel = Math.Clamp(suggestedLevel, 1, 200);
        }

        private static bool ContainsBattleKeyword(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            var lower = text.ToLowerInvariant();
            return BattleKeywords.Any(k => lower.Contains(k));
        }

        private static void ResetBossFields(StoryAiResponse response)
        {
            response.TriggerBattle = false;
            response.BossId = null;
            response.BossName = null;
            response.BossLevel = null;
        }
    }
}
