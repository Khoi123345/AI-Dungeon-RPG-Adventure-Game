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

            // Kiểm tra bossId AI gán có hợp lệ không
            bool isValidBoss = false;
            if (!string.IsNullOrWhiteSpace(response.BossId))
            {
                var checkId = response.BossId;
                isValidBoss = GameShared.Config.GameConstants.BossCatalog.Any(b => b.bossId.Equals(checkId, StringComparison.OrdinalIgnoreCase))
                              || await _contentService.BossExistsAsync(checkId);
            }

            // Tự điền hoặc ghi đè bossId nếu trống hoặc không hợp lệ
            if (!isValidBoss)
            {
                if (!string.IsNullOrWhiteSpace(response.BossId))
                {
                    _logger.LogInformation("Invalid bossId '{BossId}' provided by AI. Overwriting with fallback.", response.BossId);
                }

                var rawLoc = response.CurrentLocation ?? context.Session.currentLocation ?? context.Character.currentLocationId ?? "";
                var currentLoc = NormalizeLocationId(rawLoc);

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
                else if (currentLoc == "sunken_shipwreck")
                {
                    if (lowerText.Contains("crab") || lowerText.Contains("cua")) { response.BossId = "mob_mutated_crab"; response.BossName = "Mutated Crab"; }
                    else if (lowerText.Contains("sailor") || lowerText.Contains("thủy thủ")) { response.BossId = "mob_drowned_sailor"; response.BossName = "Drowned Sailor"; }
                    else { response.BossId = "mob_abyssal_spirit"; response.BossName = "Abyssal Spirit"; }
                }
                else if (currentLoc == "abyssal_trench")
                {
                    if (lowerText.Contains("remnant") || lowerText.Contains("tàn dư")) { response.BossId = "mob_void_remnant"; response.BossName = "Void Remnant"; }
                    else { response.BossId = "mob_abyssal_spirit"; response.BossName = "Abyssal Spirit"; }
                }
                else if (currentLoc == "coral_palace")
                {
                    response.BossId = "mob_shadow_spirit";
                    response.BossName = "Shadow Spirit";
                }
                else if (currentLoc == "sulfur_mines")
                {
                    if (lowerText.Contains("dragon") || lowerText.Contains("rồng")) { response.BossId = "mob_young_dragon"; response.BossName = "Young Dragon"; }
                    else { response.BossId = "mob_fire_lizard"; response.BossName = "Fire Lizard"; }
                }
                else if (currentLoc == "obsidian_peaks" || currentLoc == "dragon_nest")
                {
                    if (lowerText.Contains("adult") || lowerText.Contains("trưởng thành")) { response.BossId = "mob_adult_dragon"; response.BossName = "Adult Dragon"; }
                    else { response.BossId = "mob_fire_raptor"; response.BossName = "Fire Raptor"; }
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
                    var chapterBossId = NormalizeLocationId(context.Session.currentLocation) switch
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
                var loc = NormalizeLocationId(context.Session.currentLocation);
                if (loc == "coral_palace" || loc == "abyssal_trench" || loc == "sunken_shipwreck")
                {
                    response.BossId   = "mob_shadow_spirit";
                    response.BossName = "Shadow Spirit";
                }
                else if (loc == "sulfur_mines")
                {
                    response.BossId   = "mob_young_dragon";
                    response.BossName = "Young Dragon";
                }
                else if (loc == "obsidian_peaks" || loc == "dragon_nest")
                {
                    response.BossId   = "mob_adult_dragon";
                    response.BossName = "Adult Dragon";
                }
                else
                {
                    response.BossId   = "mob_goblin_guard";
                    response.BossName = "Goblin Guard";
                }
                _logger.LogInformation("Downgraded chapter boss → {BossId} for location {Location} (node={Node})", response.BossId, loc, context.Session.currentNodeId);
            }


            var id = response.BossId;
            var existsInCatalog = GameShared.Config.GameConstants.BossCatalog.Any(b => b.bossId.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (!existsInCatalog && !await _contentService.BossExistsAsync(id))
            {
                _logger.LogInformation("Rejected battle trigger because boss {BossId} does not exist in catalog or content", id);
                ResetBossFields(response);
                return;
            }

            var effectiveLocation = NormalizeLocationId(response.CurrentLocation ?? context.Session.currentLocation ?? context.Character.currentLocationId);
            if (!string.IsNullOrWhiteSpace(effectiveLocation) && !await _contentService.LocationExistsAsync(effectiveLocation))
            {
                _logger.LogInformation("Rejected battle trigger because boss {BossId} references invalid location {Location}", response.BossId, effectiveLocation);
                ResetBossFields(response);
                return;
            }


            response.BossName = string.IsNullOrWhiteSpace(response.BossName) ? response.BossId : response.BossName;
            var bossTemplate = GameShared.Config.GameConstants.BossCatalog.FirstOrDefault(b => b.bossId.Equals(response.BossId, StringComparison.OrdinalIgnoreCase));

            // Ưu tiên bossLevel AI đã gán trong prompt (ví dụ: bossLevel: 10 trong goblin_hideout.md).
            // Chỉ tính lại bằng CalculateBossLevel khi AI không gán hoặc gán giá trị <= 0.
            if (!response.BossLevel.HasValue || response.BossLevel.Value <= 0)
            {
                response.BossLevel = GameShared.Config.GameConstants.CalculateBossLevel(context.Character.level, bossTemplate?.rarity ?? "Common", response.BossId);
                _logger.LogInformation("BossLevel not set by AI, calculated fallback: {Level}", response.BossLevel);
            }
            else
            {
                response.BossLevel = Math.Clamp(response.BossLevel.Value, 1, 200);
                _logger.LogInformation("BossLevel kept from AI prompt: {Level}", response.BossLevel);
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

        private static string NormalizeLocationId(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var clean = raw.Trim().ToLowerInvariant();
            if (clean.StartsWith("location_")) clean = clean["location_".Length..];
            if (clean.StartsWith("loc_")) clean = clean["loc_".Length..];
            return clean;
        }
    }
}

