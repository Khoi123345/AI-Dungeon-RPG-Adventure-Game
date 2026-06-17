using System;
using System.Collections.Generic;

[Serializable]
public class GameUserRecord
{
    public string userId;
    public string username;
    public string email;
    public string passwordHash;
    public string displayName;
    public string status;
    public DateTime createdAt;
    public DateTime lastLoginAt;
}

[Serializable]
public class GameCharacterRecord
{
    public string characterId;
    public string userId;
    public string name;
    public int level;
    public int experience;
    public int hp;
    public int maxHp;
    public int attack;
    public int defense;
    public float critical_rate;
    public float lucky_rate;
    public int gold;
    public string className;
    public string status;
    public string currentLocationId;
    public DateTime reviveTime;
}

[Serializable]
public class GameItemRecord
{
    public string itemId;
    public string name;
    public string rarity;
    public string itemType;
    public int attackBonus;
    public int defenseBonus;
    public int hpBonus;
    public float criticalBonus;
    public string imageUrl;
    public string description;
    public bool stackable;
    public int sellPrice;
    public int buyPrice;
    public int requiredLevel;
    public string slotType;
    public string effectJson;
}

[Serializable]
public class GameInventoryRecord
{
    public string inventoryId;
    public string characterId;
    public string itemId;
    public int quantity;
    public bool equipped;
    public int slotIndex;
    public bool locked;
    public DateTime acquiredAt;
}

[Serializable]
public class GameStorySessionRecord
{
    public string sessionId;
    public string characterId;
    public string currentLocation;
    public string currentNodeId;
    public string storyContext;
    public string status;
    public DateTime updatedAt;
    public DateTime? endedAt;
    public string storyVersion;
    public string storySummary;
    public string sourceType;
}

[Serializable]
public class GameStoryActionRecord
{
    public string actionId;
    public string sessionId;
    public string playerInput;
    public string aiResponse;
    public int choiceIndex;
    public string actionType;
    public string metadataJson;
    public DateTime createdAt;
}

[Serializable]
public class GameBossRecord
{
    public string bossId;
    public string name;
    public string rarity;
    public int baseHp;
    public int baseAttack;
    public int baseDefense;
    public int speed;
    public float criticalRate;
    public string imageUrl;
    public int expReward;
    public int goldReward;
    public string skillSetJson;
    public int level;
}

[Serializable]
public class GameBossEncounterRecord
{
    public string encounterId;
    public string characterId;
    public string bossId;
    public int bossLevel;
    public int playerHpBefore;
    public int playerHpAfter;
    public int bossHpBefore;
    public int bossHpAfter;
    public string status;
    public DateTime encounterTime;
}

[Serializable]
public class GameBattleRecord
{
    public string battleId;
    public string encounterId;
    public int playerPower;
    public int bossPower;
    public string battleType;
    public string status;
    public string result;
    public int turnCount;
    public int durationMs;
    public string playerSnapshotJson;
    public string bossSnapshotJson;
    public string rewardJson;
    public DateTime battleTime;
}

[Serializable]
public class GameLootDropRecord
{
    public string lootId;
    public string battleId;
    public string itemId;
    public int quantity;
    public float dropRate;
    public string sourceType;
    public bool isUnique;
    public DateTime createdAt;
}

[Serializable]
public class GameStoryChoiceRecord
{
    public string label;
    public string description;
    public string nextNodeId;
    public int goldDelta;
    public int hpDelta;
    public int expDelta;
}