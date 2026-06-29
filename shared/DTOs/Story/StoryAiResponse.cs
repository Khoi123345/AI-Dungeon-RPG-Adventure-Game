using System.Collections.Generic;

namespace GameShared.DTOs.Story
{
    [System.Serializable]
    public class StoryAiResponse
    {
        public string NarrativeText { get; set; }

        public string CurrentNodeId { get; set; }

        public string CurrentLocation { get; set; }

        public string CurrentChapterId { get; set; }

        public string StorySummary { get; set; }

        public string ActionType { get; set; }

        public string MetadataJson { get; set; }

        public bool TriggerBattle { get; set; }

        public string BossId { get; set; }

        public string BossName { get; set; }

        public int? BossLevel { get; set; }

        public StoryAiCharacterDelta CharacterDelta { get; set; }

        public List<StoryAiInventoryChange> InventoryChanges { get; set; } = new();
    }

    [System.Serializable]
    public class StoryAiCharacterDelta
    {
        public int HpDelta { get; set; }

        public int GoldDelta { get; set; }

        public int ExpDelta { get; set; }

        public int MpDelta { get; set; }

        public string Status { get; set; }

        public string CurrentLocationId { get; set; }
    }

    [System.Serializable]
    public class StoryAiInventoryChange
    {
        public string ItemId { get; set; }

        public string ItemName { get; set; }

        public int QuantityDelta { get; set; }

        public bool Equipped { get; set; }

        public int? SlotIndex { get; set; }

        public bool Locked { get; set; }
    }
}