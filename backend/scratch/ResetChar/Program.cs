using System;
using System.IO;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using System.Collections.Generic;

class Program {
    static void Main() {
        var client = new AmazonDynamoDBClient();
        var scanReq = new ScanRequest {
            TableName = "GameCharacters",
            FilterExpression = "#n = :name",
            ExpressionAttributeNames = new Dictionary<string, string> { { "#n", "name" } },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":name", new AttributeValue { S = "khoi" } } }
        };
        var res = client.ScanAsync(scanReq).Result;
        foreach (var item in res.Items) {
            var charId = item["characterId"].S;

            // 1. Reset Stats
            var updateReq = new UpdateItemRequest {
                TableName = "GameCharacters",
                Key = new Dictionary<string, AttributeValue> { { "characterId", new AttributeValue { S = charId } } },
                UpdateExpression = "SET #lvl = :lvl, #exp = :exp, #loc = :loc, #hp = :hp, #mhp = :mhp, #atk = :atk, #def = :def",
                ExpressionAttributeNames = new Dictionary<string, string> {
                    { "#lvl", "level" },
                    { "#exp", "experience" },
                    { "#loc", "currentLocationId" },
                    { "#hp", "hp" },
                    { "#mhp", "maxHp" },
                    { "#atk", "attack" },
                    { "#def", "defense" }
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                    { ":lvl", new AttributeValue { N = "1" } },
                    { ":exp", new AttributeValue { N = "0" } },
                    { ":loc", new AttributeValue { S = "ancient_cave" } },
                    { ":hp", new AttributeValue { N = "100" } },
                    { ":mhp", new AttributeValue { N = "100" } },
                    { ":atk", new AttributeValue { N = "10" } },
                    { ":def", new AttributeValue { N = "5" } }
                }
            };
            client.UpdateItemAsync(updateReq).Wait();
            Console.WriteLine($"Reset Character {charId} ('khoi') stats to Level 1 defaults: HP 100, ATK 10, DEF 5, ancient_cave");

            // 2. Delete Story Sessions & Actions
            var sessScan = client.ScanAsync(new ScanRequest { TableName = "GameStorySessions" }).Result;
            foreach (var sessDoc in sessScan.Items) {
                if (sessDoc.ContainsKey("characterId") && sessDoc["characterId"].S == charId) {
                    var sessId = sessDoc["sessionId"].S;
                    
                    // Delete Actions for this session
                    var actScan = client.ScanAsync(new ScanRequest { TableName = "GameStoryActions" }).Result;
                    foreach (var actDoc in actScan.Items) {
                        if (actDoc.ContainsKey("sessionId") && actDoc["sessionId"].S == sessId) {
                            client.DeleteItemAsync(new DeleteItemRequest {
                                TableName = "GameStoryActions",
                                Key = new Dictionary<string, AttributeValue> { { "actionId", actDoc["actionId"] } }
                            }).Wait();
                        }
                    }

                    client.DeleteItemAsync(new DeleteItemRequest {
                        TableName = "GameStorySessions",
                        Key = new Dictionary<string, AttributeValue> { { "sessionId", new AttributeValue { S = sessId } } }
                    }).Wait();
                    Console.WriteLine($"Deleted StorySession {sessId} and its StoryActions");
                }
            }

            // 3. Clear Inventory items for character khoi
            var invScan = client.ScanAsync(new ScanRequest { TableName = "GameInventory" }).Result;
            foreach (var invDoc in invScan.Items) {
                if (invDoc.ContainsKey("characterId") && invDoc["characterId"].S == charId) {
                    client.DeleteItemAsync(new DeleteItemRequest {
                        TableName = "GameInventory",
                        Key = new Dictionary<string, AttributeValue> { { "inventoryId", invDoc["inventoryId"] } }
                    }).Wait();
                    Console.WriteLine($"Deleted Inventory item {invDoc["itemId"].S} from khoi's inventory");
                }
            }
        }
        Console.WriteLine("\nFULL STORY & SESSION RESET COMPLETED SUCCESSFULLY!");
    }
}
