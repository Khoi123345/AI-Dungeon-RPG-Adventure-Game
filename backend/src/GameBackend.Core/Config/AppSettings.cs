using System;

namespace GameBackend.Core.Config
{
    public static class AppSettings
    {
        public static string UsersTableName => Environment.GetEnvironmentVariable("USERS_TABLE") ?? "GameUsers";
        public static string CharactersTableName => Environment.GetEnvironmentVariable("CHARACTERS_TABLE") ?? "GameCharacters";
        public static string BossesTableName => Environment.GetEnvironmentVariable("BOSSES_TABLE") ?? "GameBosses";
        public static string EncountersTableName => Environment.GetEnvironmentVariable("ENCOUNTERS_TABLE") ?? "GameBossEncounters";
        public static string BattlesTableName => Environment.GetEnvironmentVariable("BATTLES_TABLE") ?? "GameBattles";
        public static string LootDropsTableName => Environment.GetEnvironmentVariable("LOOT_DROPS_TABLE") ?? "GameLootDrops";
        public static string StorySessionsTableName => Environment.GetEnvironmentVariable("STORY_SESSIONS_TABLE") ?? "GameStorySessions";
        public static string StoryActionsTableName => Environment.GetEnvironmentVariable("STORY_ACTIONS_TABLE") ?? "GameStoryActions";
        public static string InventoryTableName => Environment.GetEnvironmentVariable("INVENTORY_TABLE") ?? "GameInventory";
    }
}
