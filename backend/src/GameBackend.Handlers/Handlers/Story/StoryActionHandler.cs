using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using GameBackend.Core.Services.Interfaces;
using GameBackend.Core.Utils;
using GameBackend.Handlers.Utils;
using GameBackend.Handlers.DependencyInjection;
using GameShared.DTOs.Story;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace GameBackend.Handlers.Story
{
    public class StoryActionHandler
    {
        private readonly IStoryService _storyService;

        public StoryActionHandler()
        {
            var sp = ServiceProviderBuilder.Build();
            _storyService = sp.GetRequiredService<IStoryService>();
        }

        public async Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            if (request.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
                return ResponseBuilder.Options();

            try
            {
                var actionRequest = JsonSerializer.Deserialize<StoryActionRequest>(request.Body);
                if (actionRequest == null || string.IsNullOrWhiteSpace(actionRequest.sessionId))
                    return ResponseBuilder.Error(400, "Invalid request payload", "INVALID_REQUEST");

                var result = await _storyService.ProcessActionAsync(actionRequest);
                return ResponseBuilder.Success(result);
            }
            catch (GameNotFoundException ex)
            {
                return ResponseBuilder.Error(404, ex.Message, "NOT_FOUND");
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message} {ex.StackTrace}");
                return ResponseBuilder.Error(500, "Internal server error", "SERVER_ERROR");
            }
        }
    }
}
