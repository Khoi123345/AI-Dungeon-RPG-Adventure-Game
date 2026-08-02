using System;
using System.Collections.Generic;
using GameShared.DTOs.Character;

/// <summary>
/// Local View-Model cho ProfileScene.
/// Chứa các data class tiện dụng để ProfileView có thể render
/// mà không cần phụ thuộc trực tiếp vào shared DTOs.
/// </summary>

// ── Tab: Tổng Quan ──────────────────────────────────────────────
public class ProfileOverviewData
{
    public string characterName;
    public string className;
    public int level;
    public int experience;
    public int experienceToNextLevel;   // để tính EXP bar (exp / expToNext)
    public int hp;
    public int maxHp;
    public int mp;
    public int maxMp;
    public int gold;
    public string status;
    public string locationId;
}

// ── Tab: Chỉ Số ─────────────────────────────────────────────────
public class ProfileStatsData
{
    public int attack;
    public int defense;
    public float criticalRate;
    public float luckyRate;

    // Hidden stats
    public float speed;
    public float evasionRate;
    public float magicResist;
}

// ── Tab: Trang Bị ───────────────────────────────────────────────
public class ProfileEquipmentData
{
    public List<ProfileEquippedSlot> slots;
}

// ── Tab: Danh Hiệu ──────────────────────────────────────────────
public class ProfileTitlesData
{
    public List<ProfileTitleEntry> titles;
}

// ── Tab: Lịch Sử ────────────────────────────────────────────────
public class ProfileHistoryData
{
    public List<AdventureRecord> records;
}

// ── Wrapper tổng hợp ─────────────────────────────────────────────
public class ProfileData
{
    public ProfileOverviewData overview;
    public ProfileStatsData stats;
    public ProfileEquipmentData equipment;
    public ProfileTitlesData titles;
    public ProfileHistoryData history;

    /// <summary>
    /// Chuyển đổi từ DTO tổng hợp sang ProfileData local model.
    /// </summary>
    public static ProfileData FromDTO(ProfileCharacterData dto)
    {
        if (dto == null) return null;

        return new ProfileData
        {
            overview = new ProfileOverviewData
            {
                characterName       = dto.characterName,
                className           = dto.className,
                level               = dto.level,
                experience          = dto.experience,
                experienceToNextLevel = dto.experienceToNextLevel,
                hp                  = dto.hp,
                maxHp               = dto.maxHp,
                mp                  = dto.mp,
                maxMp               = dto.maxMp,
                gold                = dto.gold,
                status              = dto.status,
                locationId          = dto.currentLocationId
            },
            stats = new ProfileStatsData
            {
                attack       = dto.attack,
                defense      = dto.defense,
                criticalRate = dto.criticalRate,
                luckyRate    = dto.luckyRate,
                speed        = dto.speed,
                evasionRate  = dto.evasionRate,
                magicResist  = dto.magicResist
            },
            equipment = new ProfileEquipmentData
            {
                slots = dto.equippedSlots ?? new List<ProfileEquippedSlot>()
            },
            titles = new ProfileTitlesData
            {
                titles = dto.titles ?? new List<ProfileTitleEntry>()
            },
            history = new ProfileHistoryData
            {
                records = dto.adventureHistory ?? new List<AdventureRecord>()
            }
        };
    }
}
