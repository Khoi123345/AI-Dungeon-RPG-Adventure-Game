using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using System.Collections.Generic;
using System;

namespace GameBackend.Handlers.Utils
{
    /// <summary>
    /// Chuẩn hóa tất cả API Gateway response với CORS headers và JSON body.
    /// </summary>
    public static class ResponseBuilder
    {
        private static readonly Dictionary<string, string> CorsHeaders = new()
        {
            { "Access-Control-Allow-Origin", "*" },
            { "Access-Control-Allow-Headers", "Content-Type,Authorization" },
            { "Access-Control-Allow-Methods", "GET,POST,PUT,DELETE,OPTIONS" },
            { "Content-Type", "application/json" }
        };

        public static APIGatewayProxyResponse Success<T>(T data, string message = "Success")
        {
            var body = new { success = true, message, data };
            return new APIGatewayProxyResponse
            {
                StatusCode = 200,
                Headers = CorsHeaders,
                Body = JsonSerializer.Serialize(body)
            };
        }

        public static APIGatewayProxyResponse Error(int statusCode, string message, string? errorCode = null)
        {
            var body = new { success = false, message, errorCode = errorCode ?? "UNKNOWN_ERROR" };
            return new APIGatewayProxyResponse
            {
                StatusCode = statusCode,
                Headers = CorsHeaders,
                Body = JsonSerializer.Serialize(body)
            };
        }

        public static APIGatewayProxyResponse Options()
        {
            return new APIGatewayProxyResponse { StatusCode = 200, Headers = CorsHeaders };
        }
    }
}
