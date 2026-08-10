import boto3
import json

dynamodb = boto3.resource('dynamodb', region_name='ap-southeast-1')
table = dynamodb.Table('GameInventory')

response = table.scan(
    FilterExpression="characterId = :c",
    ExpressionAttributeValues={":c": "9da950e813be40b0ba2c85ddeefd6d3f"}
)

print(json.dumps(response.get('Items', []), indent=2, ensure_ascii=False))
