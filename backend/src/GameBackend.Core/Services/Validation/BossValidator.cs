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
            "đánh boss", "đánh quái", "vào trận", "thách đấu", "giương kiếm", "đối mặt", "chém", "tiêu diệt"
        };

        public BossValidator(IContentService contentService, ILogger<BossValidator> logger)
        {
            _contentService = contentService;
            _logger = logger;
        }

        public async Task ValidateAsync(GameRuleValidationContext context)
        {
            var response = context.Response;
            var lowerText = (response.NarrativeText ?? string.Empty).ToLowerInvariant();

            bool isCombatText = BattleKeywords.Any(kw => lowerText.Contains(kw, StringComparison.OrdinalIgnoreCase));
            bool isInBossRoom = string.Equals(context.Session.currentNodeId, "boss_room", StringComparison.OrdinalIgnoreCase);

            if (isInBossRoom || isCombatText)
            {
                response.TriggerBattle = true;
            }

            if (!response.TriggerBattle)
            {
                ResetBossFields(response);
                return;
            }

            // Chỉ tự điền bossId khi AI không gán — KHÔNG ghi đè bossId AI đã chỉ định (kể cả mob_*)
            if (string.IsNullOrWhiteSpace(response.BossId))
            {
                var currentLoc = (context.Session.currentLocation ?? context.Character.currentLocationId ?? "").ToLowerInvariant();

                if (currentLoc == "forgotten_temple")
                {
                    if (lowerText.Contains("golem")) { response.BossId = "mob_temple_golem"; response.BossName = "Temple Golem"; }
                    else if (lowerText.Contains("guard") || lowerText.Contains("vệ binh")) { response.BossId = "mob_goblin_guard"; response.BossName = "Goblin Guard"; }
                    else { response.BossId = "mob_shadow_spirit"; response.BossName = "Shadow Spirit"; }
                }
                else if (currentLoc == "goblin_hideout")
                {
                    response.BossId = "mob_goblin_guard";
                    response.BossName = "Goblin Guard";
                }
                else
                {
                    if (lowerText.Contains("spider") || lowerText.Contains("nhện")) { response.BossId = "mob_cave_spider"; response.BossName = "Cave Spider"; }
                    else { response.BossId = "mob_goblin_scout"; response.BossName = "Goblin Scout"; }
                }
            }

            // Chapter boss chỉ được phép khi player đang ở boss_room
            bool isChapterBoss = (response.BossId ?? "").Contains("goblin_king", StringComparison.OrdinalIgnoreCase)
                               || (response.BossId ?? "").Contains("shadow_demon", StringComparison.OrdinalIgnoreCase)
                               || (response.BossId ?? "").Contains("dragon_king", StringComparison.OrdinalIgnoreCase);

            // Nếu ở boss_room mà AI không set triggerBattle (hoặc không set bossId) → tự động ép
            if (isInBossRoom)
            {
                response.TriggerBattle = true;
                if (string.IsNullOrWhiteSpace(response.BossId) || !isChapterBoss)
                {
                    // Gán chapter boss mặc định dựa theo location
                    var chapterBossId = (context.Session.currentLocation ?? "").ToLowerInvariant() switch
                    {
                        "coral_palace" => "boss_shadow_demon",
                        "dragon_nest"  => "boss_dragon_king",
                        _              => "boss_goblin_king"
                    };
                    response.BossId = chapterBossId;
                    _logger.LogInformation("boss_room auto-forced triggerBattle=true with bossId={BossId}", chapterBossId);
                }
            }
            else if (isChapterBoss && !isInBossRoom)
            {
                _logger.LogInformation("Downgraded chapter boss → mob_goblin_guard: player not in boss_room (node={Node})", context.Session.currentNodeId);
                response.BossId   = "mob_goblin_guard";
                response.BossName = "Goblin Guard";
            }

            var id = response.BossId;
            var existsInCatalog = GameShared.Config.GameConstants.BossCatalog.Any(b => b.bossId.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (!existsInCatalog && !await _contentService.BossExistsAsync(id))
            {
                _logger.LogInformation("Rejected battle trigger because boss {BossId} does not exist in catalog or content", id);
                ResetBossFields(response);
                return;
            }

            var effectiveLocation = response.CurrentLocation ?? context.Session.currentLocation ?? context.Character.currentLocationId;
            if (!string.IsNullOrWhiteSpace(effectiveLocation) && !await _contentService.LocationExistsAsync(effectiveLocation))
            {
                _logger.LogInformation("Rejected battle trigger because boss {BossId} references invalid location {Location}", response.BossId, effectiveLocation);
                ResetBossFields(response);
                return;
            }

            response.BossName = string.IsNullOrWhiteSpace(response.BossName) ? response.BossId : response.BossName;
            var bossTemplate = GameShared.Config.GameConstants.BossCatalog.FirstOrDefault(b => b.bossId.Equals(response.BossId, StringComparison.OrdinalIgnoreCase));
            var calculatedLvl = GameShared.Config.GameConstants.CalculateBossLevel(context.Character.level, bossTemplate?.rarity ?? "Common", response.BossId);
            
            if (!response.BossLevel.HasValue || response.BossLevel.Value <= 0 || response.BossLevel.Value == context.Character.level)
            {
                response.BossLevel = calculatedLvl;
            }
            else
            {
                response.BossLevel = Math.Clamp(response.BossLevel.Value, 1, 200);
            }
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
