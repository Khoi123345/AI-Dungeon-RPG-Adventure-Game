using GameBackend.Core.AIStory.Services;
using GameBackend.Core.Repositories.Interfaces;
using GameShared.DTOs.Story;
using GameShared.Models;
using Microsoft.Extensions.Logging;

namespace GameBackend.Core.Services.Validation
{
    public sealed class BossValidator : IGameRuleSubValidator
    {
        private readonly IContentService _contentService;
        private readonly IDefeatedBossRepository _defeatedBossRepository;
        private readonly ILogger<BossValidator> _logger;

        private static readonly string[] BattleKeywords = new[]
        {
            "trận chiến", "giao chiến", "tấn công", "chiến đấu", "khiêu chiến",
            "bắt đầu trận", "quái vật", "sói bóng tối", "gặp boss", "vua goblin",
            "đánh boss", "đánh quái", "vào trận", "thách đấu", "giương kiếm", "đối mặt", "chém", "tiêu diệt"
        };

        public BossValidator(
            IContentService contentService,
            IDefeatedBossRepository defeatedBossRepository,
            ILogger<BossValidator> logger)
        {
            _contentService = contentService;
            _defeatedBossRepository = defeatedBossRepository;
            _logger = logger;
        }

        public async Task ValidateAsync(GameRuleValidationContext context)
        {
            var response = context.Response;
            var lowerText = (response.NarrativeText ?? string.Empty).ToLowerInvariant();
            string effectiveNode = response.CurrentNodeId ?? context.Session.currentNodeId ?? "";
            bool isInBossRoom = string.Equals(effectiveNode, "boss_room", StringComparison.OrdinalIgnoreCase);

            var rawLoc = response.CurrentLocation ?? context.Session.currentLocation ?? context.Character.currentLocationId ?? "";
            var currentLoc = NormalizeLocationId(rawLoc);

            var defeatedBosses = await GetDefeatedBossesSafelyAsync(context.Character.characterId);
            RemoveDefeatedBossChoices(response, defeatedBosses, currentLoc);

            bool isBossLocation = isInBossRoom;

            // Phân biệt: "người chơi bị đánh bại" vs "boss/quái bị đánh bại"
            bool playerWasDefeated = lowerText.Contains("đánh bại bạn") ||
                                     lowerText.Contains("bạn bị đánh bại") ||
                                     lowerText.Contains("hạ gục bạn") ||
                                     lowerText.Contains("đã đánh bại người chơi") ||
                                     lowerText.Contains("bạn đã ngã xuống") ||
                                     lowerText.Contains("đánh bại người chơi");

            bool narrativeSaysBossDefeated = !playerWasDefeated && (
                                            lowerText.Contains("boss đã bị đánh bại") ||
                                            lowerText.Contains("quái vật bị đánh bại") ||
                                            lowerText.Contains("kẻ địch bị đánh bại") ||
                                            lowerText.Contains("quái vật tan biến") ||
                                            lowerText.Contains("kẻ địch tan biến") ||
                                            lowerText.Contains("shadow demon ngã xuống") ||
                                            lowerText.Contains("goblin king ngã xuống") ||
                                            lowerText.Contains("dragon king ngã xuống") ||
                                            lowerText.Contains("đã tiêu diệt") ||
                                            lowerText.Contains("sau khi đánh bại") ||
                                            (lowerText.Contains("đã đánh bại") && !lowerText.Contains("đánh bại bạn")));

            // Kiểm tra người chơi có đang chủ động đối mặt / tái chiến Boss hay không
            bool isConfrontingBoss = lowerText.Contains("đối mặt với shadow demon") ||
                                     lowerText.Contains("đối mặt với goblin king") ||
                                     lowerText.Contains("đối mặt với dragon king") ||
                                     (lowerText.Contains("shadow demon") && (lowerText.Contains("đối mặt") || lowerText.Contains("chiến đấu") || lowerText.Contains("tấn công") || lowerText.Contains("khiêu chiến"))) ||
                                     (lowerText.Contains("goblin king") && (lowerText.Contains("đối mặt") || lowerText.Contains("chiến đấu") || lowerText.Contains("tấn công") || lowerText.Contains("khiêu chiến"))) ||
                                     (lowerText.Contains("dragon king") && (lowerText.Contains("đối mặt") || lowerText.Contains("chiến đấu") || lowerText.Contains("tấn công") || lowerText.Contains("khiêu chiến")));

            if (playerWasDefeated)
            {
                response.TriggerBattle = false;
                ResetBossFields(response);
                _logger.LogInformation("Forcing TriggerBattle=false and resetting boss fields because player was defeated in battle narrative.");
                return;
            }
            else if (isConfrontingBoss && isInBossRoom && !narrativeSaysBossDefeated)
            {
                response.TriggerBattle = true;
                _logger.LogInformation("Forcing TriggerBattle=true because player is confronting boss in boss_room.");
            }

            if (!response.TriggerBattle)
            {
                ResetBossFields(response);
                return;
            }

            // Nếu AI gán sẵn mob_ (quái thường), bảo toàn ID quái thường và không nâng cấp lên Chapter Boss
            bool isMobRequested = (response.BossId ?? "").StartsWith("mob_", StringComparison.OrdinalIgnoreCase);

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
                    _logger.LogInformation("Invalid bossId '{BossId}' provided by AI. Rejecting battle trigger.", response.BossId);
                    ResetBossFields(response);
                    return;
                }

                if (currentLoc == "forgotten_temple")
                {
                    if (lowerText.Contains("golem")) { response.BossId = "mob_temple_golem"; response.BossName = "Temple Golem"; }
                    else if (lowerText.Contains("guard") || lowerText.Contains("vệ binh")) { response.BossId = "mob_goblin_guard"; response.BossName = "Goblin Guard"; }
                    else { response.BossId = "mob_shadow_spirit"; response.BossName = "Shadow Spirit"; }
                }
                else if (currentLoc == "goblin_hideout")
                {
                    if (isInBossRoom) { response.BossId = "boss_goblin_king"; response.BossName = "Goblin King"; }
                    else { response.BossId = "mob_goblin_guard"; response.BossName = "Goblin Guard"; }
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
                    if (isInBossRoom) { response.BossId = "boss_shadow_demon"; response.BossName = "Shadow Demon"; }
                    else { response.BossId = "mob_abyssal_spirit"; response.BossName = "Abyssal Spirit"; }
                }
                else if (currentLoc == "sulfur_mines")
                {
                    if (lowerText.Contains("dragon") || lowerText.Contains("rồng")) { response.BossId = "mob_young_dragon"; response.BossName = "Young Dragon"; }
                    else { response.BossId = "mob_fire_lizard"; response.BossName = "Fire Lizard"; }
                }
                else if (currentLoc == "obsidian_peaks")
                {
                    if (lowerText.Contains("adult") || lowerText.Contains("trưởng thành")) { response.BossId = "mob_adult_dragon"; response.BossName = "Adult Dragon"; }
                    else { response.BossId = "mob_fire_raptor"; response.BossName = "Fire Raptor"; }
                }
                else if (currentLoc == "dragon_nest")
                {
                    if (isInBossRoom) { response.BossId = "boss_dragon_king"; response.BossName = "Dragon King"; }
                    else { response.BossId = "mob_adult_dragon"; response.BossName = "Adult Dragon"; }
                }
                else
                {
                    if (lowerText.Contains("spider") || lowerText.Contains("nhện")) { response.BossId = "mob_cave_spider"; response.BossName = "Cave Spider"; }
                    else { response.BossId = "mob_goblin_scout"; response.BossName = "Goblin Scout"; }
                }
            }

            // Chapter boss check
            bool isChapterBoss = (response.BossId ?? "").Contains("goblin_king", StringComparison.OrdinalIgnoreCase)
                               || (response.BossId ?? "").Contains("shadow_demon", StringComparison.OrdinalIgnoreCase)
                               || (response.BossId ?? "").Contains("dragon_king", StringComparison.OrdinalIgnoreCase);

            // RÀNG BUỘC SẮT: Nếu lời văn AI mô tả trận đấu ĐÃ KẾT THÚC hoặc YÊU CẦU NGƯỜI CHƠI ĐƯA RA QUYẾT ĐỊNH LỰA CHỌN TIẾP THEO -> BẮT BUỘC ĐẶT triggerBattle = false
            string narrativeLower = (response.NarrativeText ?? "").ToLowerInvariant();
            if (narrativeLower.Contains("sau khi đánh bại") ||
                (narrativeLower.Contains("đã đánh bại") && !playerWasDefeated) ||
                narrativeLower.Contains("đã tiêu diệt") ||
                narrativeSaysBossDefeated ||
                narrativeLower.Contains("mở nó ra") ||
                narrativeLower.Contains("quyết định tiếp theo") ||
                narrativeLower.Contains("bạn phải quyết định") ||
                narrativeLower.Contains("bạn muốn làm gì"))
            {
                if (response.TriggerBattle)
                {
                    bool isJustAsking = narrativeLower.Contains("quyết định tiếp theo") || 
                                        narrativeLower.Contains("bạn phải quyết định") || 
                                        narrativeLower.Contains("bạn muốn làm gì") ||
                                        narrativeLower.Contains("mở nó ra");
                                        
                    if (!narrativeSaysBossDefeated && (isJustAsking || isConfrontingBoss || isInBossRoom))
                    {
                        _logger.LogInformation("Skipping TriggerBattle sanitization because player is in boss room or confronting boss.");
                    }
                    else
                    {
                        _logger.LogInformation("Sanitizing TriggerBattle from TRUE -> FALSE because narrative describes combat resolution.");
                        ResetBossFields(response);
                    }
                }
            }

            if (!isMobRequested && (isInBossRoom || isConfrontingBoss) && !narrativeSaysBossDefeated && response.TriggerBattle)
            {
                if (string.IsNullOrWhiteSpace(response.BossId) || !isChapterBoss)
                {
                    // Gán chapter boss mặc định dựa theo location khi ở trong boss_room
                    var chapterBossId = currentLoc switch
                    {
                        "coral_palace" => "boss_shadow_demon",
                        "dragon_nest"  => "boss_dragon_king",
                        "goblin_hideout" => "boss_goblin_king",
                        _ => null
                    };
                    if (chapterBossId == null)
                    {
                        _logger.LogWarning("Rejected chapter boss fallback outside a canonical boss location: {Location}", currentLoc);
                        ResetBossFields(response);
                        return;
                    }
                    var chapterBossName = chapterBossId switch
                    {
                        "boss_shadow_demon" => "Shadow Demon",
                        "boss_dragon_king"  => "Dragon King",
                        _                   => "Goblin King"
                    };
                    response.BossId = chapterBossId;
                    response.BossName = chapterBossName;
                    _logger.LogInformation("Assigned chapter bossId={BossId}, bossName={BossName}", chapterBossId, chapterBossName);
                }
            }
            else if (isChapterBoss && !isInBossRoom && !isConfrontingBoss)
            {
                var loc = currentLoc;
                if (loc == "abyssal_trench" || loc == "sunken_shipwreck")
                {
                    response.BossId   = "mob_shadow_spirit";
                    response.BossName = "Shadow Spirit";
                }
                else if (loc == "sulfur_mines")
                {
                    response.BossId   = "mob_young_dragon";
                    response.BossName = "Young Dragon";
                }
                else if (loc == "obsidian_peaks")
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

            if (IsDefeatedChapterBoss(id, defeatedBosses))
            {
                _logger.LogWarning(
                    "Rejected battle trigger because boss {BossId} was already defeated by character {CharacterId}",
                    id,
                    context.Character.characterId);
                ResetBossFields(response);
                EnsureContinueChoice(response);
                return;
            }

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

        private async Task<List<DefeatedBoss>> GetDefeatedBossesSafelyAsync(string? characterId)
        {
            if (string.IsNullOrWhiteSpace(characterId)) return new List<DefeatedBoss>();

            try
            {
                return await _defeatedBossRepository.GetDefeatedBossesByCharacterIdAsync(characterId)
                       ?? new List<DefeatedBoss>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load defeated bosses for character {CharacterId}", characterId);
                return new List<DefeatedBoss>();
            }
        }

        private static void RemoveDefeatedBossChoices(
            StoryAiResponse response,
            IReadOnlyCollection<DefeatedBoss> defeatedBosses,
            string currentLocation)
        {
            if (response.Choices == null || response.Choices.Count == 0 || defeatedBosses.Count == 0) return;

            var defeatedIds = defeatedBosses
                .Where(b => !string.IsNullOrWhiteSpace(b.bossId))
                .Select(b => b.bossId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var locationBossId = currentLocation switch
            {
                "goblin_hideout" => "boss_goblin_king",
                "coral_palace" => "boss_shadow_demon",
                "dragon_nest" => "boss_dragon_king",
                _ => string.Empty
            };

            response.Choices.RemoveAll(choice =>
            {
                var text = $"{choice?.label} {choice?.description} {choice?.nextNodeId}".ToLowerInvariant();
                if (string.Equals(choice?.nextNodeId, "boss_room", StringComparison.OrdinalIgnoreCase) &&
                    defeatedIds.Contains(locationBossId))
                {
                    return true;
                }

                foreach (var defeated in defeatedBosses)
                {
                    if (string.IsNullOrWhiteSpace(defeated.bossId) ||
                        !defeated.bossId.StartsWith("boss_", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (text.Contains(defeated.bossId.ToLowerInvariant()) ||
                        (!string.IsNullOrWhiteSpace(defeated.bossName) && text.Contains(defeated.bossName.ToLowerInvariant())) ||
                        ChoiceMentionsBossAlias(text, defeated.bossId))
                    {
                        return true;
                    }
                }

                return false;
            });

            if (response.Choices.Count == 0)
            {
                EnsureContinueChoice(response);
            }
        }

        private static bool ChoiceMentionsBossAlias(string text, string bossId)
        {
            return bossId.ToLowerInvariant() switch
            {
                "boss_goblin_king" => text.Contains("goblin king") || text.Contains("vua goblin") || text.Contains("vua yêu tinh"),
                "boss_shadow_demon" => text.Contains("shadow demon") || text.Contains("ác ma bóng tối") || text.Contains("demon bóng tối"),
                "boss_dragon_king" => text.Contains("dragon king") || text.Contains("vua rồng"),
                _ => false
            };
        }

        private static bool IsDefeatedChapterBoss(string? bossId, IEnumerable<DefeatedBoss> defeatedBosses)
        {
            return !string.IsNullOrWhiteSpace(bossId) &&
                   bossId.StartsWith("boss_", StringComparison.OrdinalIgnoreCase) &&
                   defeatedBosses.Any(b => string.Equals(b.bossId, bossId, StringComparison.OrdinalIgnoreCase));
        }

        private static void EnsureContinueChoice(StoryAiResponse response)
        {
            response.Choices ??= new List<StoryChoiceOption>();
            if (response.Choices.Count > 0) return;

            response.Choices.Add(new StoryChoiceOption
            {
                label = "Tiếp tục hành trình",
                description = "Rời khỏi chiến trường đã yên tĩnh và tiến về phía trước.",
                nextNodeId = "continue_after_boss"
            });
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

