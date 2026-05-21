using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.AI;

namespace Hearth.AspNetCore;

internal static class ModelsEndpoint
{
    internal static IResult Handle(IChatClient chatClient)
    {
        var metadata = chatClient.GetService(typeof(ChatClientMetadata)) as ChatClientMetadata;
        var modelId = metadata?.DefaultModelId ?? "hearth-local";

        var response = new OpenAiModelsListResponse
        {
            Data =
            [
                new OpenAiModelInfo
                {
                    Id = modelId,
                    Created = 0,
                    OwnedBy = "hearth",
                }
            ],
        };

        return TypedResults.Ok(response);
    }
}
