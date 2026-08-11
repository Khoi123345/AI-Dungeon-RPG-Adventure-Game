using System;
using System.IO;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Collections.Generic;

class Program {
    static void Main() {
        var client = new AmazonDynamoDBClient();

        // 1. Find all characters with name "khoi"
        var scanReq = new ScanRequest {
            TableName = "GameCharacters",
            FilterExpression = "#n = :name",
            ExpressionAttributeNames = new Dictionary<string, string> { { "#n", "name" } },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":name", new AttributeValue { S = "khoi" } } }
        };
        var res = client.ScanAsync(scanReq).Result;
        var charIds = res.Items.Select(i => i["characterId"].S).ToHashSet();
        Console.WriteLine($"Found {charIds.Count} character(s) with name 'khoi'");

        // 1a. Reset Stats for all 'khoi' characters
        foreach (var charId in charIds) {
            var updateReq = new UpdateItemRequest {
                TableName = "GameCharacters",
                Key = new Dictionary<string, AttributeValue> { { "characterId", new AttributeValue { S = charId } } },
                UpdateExpression = "SET #lvl = :lvl, #exp = :exp, #loc = :loc, #hp = :hp, #mhp = :mhp, #atk = :atk, #def = :def, #gold = :gold",
                ExpressionAttributeNames = new Dictionary<string, string> {
                    { "#lvl", "level" },
                    { "#exp", "experience" },
                    { "#loc", "currentLocationId" },
                    { "#hp", "hp" },
                    { "#mhp", "maxHp" },
                    { "#atk", "attack" },
                    { "#def", "defense" },
                    { "#gold", "gold" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    { ":lvl", new AttributeValue { N = "1" } },
                    { ":exp", new AttributeValue { N = "0" } },
                    { ":loc", new AttributeValue { S = "ancient_cave" } },
                    { ":hp", new AttributeValue { N = "100" } },
                    { ":mhp", new AttributeValue { N = "100" } },
                    { ":atk", new AttributeValue { N = "15" } },
                    { ":def", new AttributeValue { N = "5" } },
                    { ":gold", new AttributeValue { N = "50" } }
                }
            };
            client.UpdateItemAsync(updateReq).Wait();
        }
        Console.WriteLine("Stats reset completed for all 'khoi' characters.");

        // 2. Scan GameStorySessions once, find sessionIds belonging to 'khoi'
        var sessionIds = new HashSet<string>();
        var sessScan = client.ScanAsync(new ScanRequest { TableName = "GameStorySessions" }).Result;
        foreach (var sessDoc in sessScan.Items) {
            if (sessDoc.ContainsKey("characterId") && charIds.Contains(sessDoc["characterId"].S)) {
                var sessId = sessDoc["sessionId"].S;
                sessionIds.Add(sessId);
                client.DeleteItemAsync(new DeleteItemRequest {
                    TableName = "GameStorySessions",
                    Key = new Dictionary<string, AttributeValue> { { "sessionId", new AttributeValue { S = sessId } } }
                }).Wait();
            }
        }
        Console.WriteLine($"Deleted {sessionIds.Count} StorySessions for character 'khoi'.");

        // 3. Delete StoryActions for those sessionIds
        int deletedActions = 0;
        var actScan = client.ScanAsync(new ScanRequest { TableName = "GameStoryActions" }).Result;
        foreach (var actDoc in actScan.Items) {
            if (actDoc.ContainsKey("sessionId") && sessionIds.Contains(actDoc["sessionId"].S)) {
                client.DeleteItemAsync(new DeleteItemRequest {
                    TableName = "GameStoryActions",
                    Key = new Dictionary<string, AttributeValue> { { "actionId", actDoc["actionId"] } }
                }).Wait();
                deletedActions++;
            }
        }
        Console.WriteLine($"Deleted {deletedActions} StoryActions.");

        // 4. Delete Inventory items for character 'khoi'
        int deletedInv = 0;
        var invScan = client.ScanAsync(new ScanRequest { TableName = "GameInventory" }).Result;
        foreach (var invDoc in invScan.Items) {
            if (invDoc.ContainsKey("characterId") && charIds.Contains(invDoc["characterId"].S)) {
                client.DeleteItemAsync(new DeleteItemRequest {
                    TableName = "GameInventory",
                    Key = new Dictionary<string, AttributeValue> { { "inventoryId", invDoc["inventoryId"] } }
                }).Wait();
                deletedInv++;
            }
        }
        Console.WriteLine($"Deleted {deletedInv} Inventory items.");

        // 5. Delete GameBossEncounters for character 'khoi'
        int deletedEnc = 0;
        var encounterIds = new HashSet<string>();
        var encScan = client.ScanAsync(new ScanRequest { TableName = "GameBossEncounters" }).Result;
        foreach (var encDoc in encScan.Items) {
            if (encDoc.ContainsKey("characterId") && charIds.Contains(encDoc["characterId"].S)) {
                encounterIds.Add(encDoc["encounterId"].S);
                client.DeleteItemAsync(new DeleteItemRequest {
                    TableName = "GameBossEncounters",
                    Key = new Dictionary<string, AttributeValue> { { "encounterId", encDoc["encounterId"] } }
                }).Wait();
                deletedEnc++;
            }
        }
        Console.WriteLine($"Deleted {deletedEnc} BossEncounters.");

        // 6. Delete persisted chapter-boss victories. Without this step, a freshly
        // reset character is still rejected by SpawnBoss as "already defeated".
        int deletedDefeatedBosses = 0;
        var defeatedScan = client.ScanAsync(new ScanRequest { TableName = "GameDefeatedBosses" }).Result;
        foreach (var defeatedDoc in defeatedScan.Items) {
            if (defeatedDoc.ContainsKey("characterId") &&
                defeatedDoc.ContainsKey("bossId") &&
                charIds.Contains(defeatedDoc["characterId"].S)) {
                client.DeleteItemAsync(new DeleteItemRequest {
                    TableName = "GameDefeatedBosses",
                    Key = new Dictionary<string, AttributeValue> {
                        { "characterId", defeatedDoc["characterId"] },
                        { "bossId", defeatedDoc["bossId"] }
                    }
                }).Wait();
                deletedDefeatedBosses++;
            }
        }
        Console.WriteLine($"Deleted {deletedDefeatedBosses} DefeatedBoss records.");

        // 7. Delete battle and loot history linked to the encounters being reset.
        var battleIds = new HashSet<string>();
        int deletedBattles = 0;
        var battleScan = client.ScanAsync(new ScanRequest { TableName = "GameBattles" }).Result;
        foreach (var battleDoc in battleScan.Items) {
            if (battleDoc.ContainsKey("encounterId") && encounterIds.Contains(battleDoc["encounterId"].S)) {
                battleIds.Add(battleDoc["battleId"].S);
                client.DeleteItemAsync(new DeleteItemRequest {
                    TableName = "GameBattles",
                    Key = new Dictionary<string, AttributeValue> { { "battleId", battleDoc["battleId"] } }
                }).Wait();
                deletedBattles++;
            }
        }
        Console.WriteLine($"Deleted {deletedBattles} Battles.");

        int deletedLootDrops = 0;
        var lootScan = client.ScanAsync(new ScanRequest { TableName = "GameLootDrops" }).Result;
        foreach (var lootDoc in lootScan.Items) {
            if (lootDoc.ContainsKey("battleId") && battleIds.Contains(lootDoc["battleId"].S)) {
                client.DeleteItemAsync(new DeleteItemRequest {
                    TableName = "GameLootDrops",
                    Key = new Dictionary<string, AttributeValue> { { "lootId", lootDoc["lootId"] } }
                }).Wait();
                deletedLootDrops++;
            }
        }
        Console.WriteLine($"Deleted {deletedLootDrops} LootDrops.");

        Console.WriteLine("\n🎉 RESET SUCCESSFUL! Character 'khoi' is completely reset to Level 1 fresh start.");
    }
}
