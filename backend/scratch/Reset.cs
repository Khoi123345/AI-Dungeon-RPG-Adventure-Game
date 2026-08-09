using System;
using System.IO;
using System.Linq;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

class Program {
    static void Main() {
        var client = new AmazonDynamoDBClient();
        var scanReq = new ScanRequest {
            TableName = ""GameCharacters"",
            FilterExpression = ""#n = :name"",
            ExpressionAttributeNames = new System.Collections.Generic.Dictionary<string, string> { { ""#n"", ""name"" } },
            ExpressionAttributeValues = new System.Collections.Generic.Dictionary<string, AttributeValue> { { "":name"", new AttributeValue { S = ""khoi"" } } }
        };
        var res = client.ScanAsync(scanReq).Result;
        foreach (var item in res.Items) {
            var charId = item[""characterId""].S;
            var updateReq = new UpdateItemRequest {
                TableName = ""GameCharacters"",
                Key = new System.Collections.Generic.Dictionary<string, AttributeValue> { { ""characterId"", new AttributeValue { S = charId } } },
                UpdateExpression = ""SET #lvl = :lvl, #exp = :exp, #loc = :loc, #hp = :hp"",
                ExpressionAttributeNames = new System.Collections.Generic.Dictionary<string, string> {
                    { ""#lvl"", ""level"" }, { ""#exp"", ""experience"" }, { ""#loc"", ""currentLocationId"" }, { ""#hp"", ""hp"" }
                },
                ExpressionAttributeValues = new System.Collections.Generic.Dictionary<string, AttributeValue> {
                    { "":lvl"", new AttributeValue { N = ""1"" } },
                    { "":exp"", new AttributeValue { N = ""0"" } },
                    { "":loc"", new AttributeValue { S = ""ancient_cave"" } },
                    { "":hp"", new AttributeValue { N = ""100"" } }
                }
            };
            client.UpdateItemAsync(updateReq).Wait();
            Console.WriteLine($""Reset {charId} to Level 1"");
        }
    }
}
