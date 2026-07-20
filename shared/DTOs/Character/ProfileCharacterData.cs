using System;
using System.Collections.Generic;

namespace GameShared.DTOs.Character
{
    /// <summary>
    /// DTO tổng hợp dữ liệu cho ProfileScene.
    /// Chứa toàn bộ thông tin mà các tab trong màn hình Profile cần hiển thị.
    /// Gộp từ Character + Inventory + Titles + AdventureHistory.
    /// </summary>
    [Serializable]
    public class ProfileCharacterData
    {
        // ── Tab: Tổng Quan ─────────────────────────────────────
        public string characterId;
        public string characterName;
        public string className;
        public int level;
        public int experience;
        public int experienceToNextLevel;   // level * 100
        public int hp;
        public int maxHp;
        public int mp;
        public int maxMp;
        public int gold;
        public string status;
        public string currentLocationId;

        // ── Tab: Chỉ Số Chiến Đấu ─────────────────────────────
        public int attack;
        public int defense;
        public float criticalRate;
        public float luckyRate;

        // Hidden stats
        public float speed;
        public float evasionRate;
        public float magicResist;

        // ── Tab: Trang Bị ──────────────────────────────────────
        public List<ProfileEquippedSlot> equippedSlots;

        // ── Tab: Danh Hiệu ─────────────────────────────────────
        public List<ProfileTitleEntry> titles;

        // ── Tab: Lịch Sử Phiêu Lưu ────────────────────────────
        public List<AdventureRecord> adventureHistory;
    }

    /// <summary>Một slot trang bị (Weapon / Armor / Accessory).</summary>
    [Serializable]
    public class ProfileEquippedSlot
    {
        public string slotType;         // "Weapon" | "Armor" | "Accessory" | "Ring" | ...
        public bool isEmpty;
        public string itemId;
        public string itemName;
        public string itemRarity;
        public string itemDescription;
        public int attackBonus;
        public int defenseBonus;
        public int hpBonus;
        public float criticalBonus;
    }

    /// <summary>Entry danh hiệu cho Profile tab.</summary>
    [Serializable]
    public class ProfileTitleEntry
    {
        public string titleId;
        public string name;
        public string description;
        public string rarity;
        public bool isEquipped;
        public DateTime earnedAt;
    }

    /// <summary>
    /// Một bản ghi lịch sử phiêu lưu (tối đa 10 bản gần nhất).
    /// Kết hợp dữ liệu từ BossEncounter + Battle.
    /// </summary>
    [Serializable]
    public class AdventureRecord
    {
        public string encounterId;
        public string bossName;
        public string bossRarity;
        public int bossLevel;
        public string result;           // "Victory" | "Defeat"
        public int expGained;
        public int goldGained;
        public int turnCount;
        public DateTime encounterTime;
    }
}
