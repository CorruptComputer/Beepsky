using Beepsky.Core.Services;

namespace Beepsky.Core.Features.Chatty;

/// <inheritdoc />
public sealed class GetBeepskyChatResponse(LLMService llmService) : IRequestHandler<GetBeepskyChatResponse.Query, QueryResponse<string>>
{

    /// <summary>
    ///   Handles a direct mention of Beepsky
    /// </summary>
    /// <param name="AuthorId"></param>
    /// <param name="GuildId"></param>
    /// <param name="Content"></param>
    /// <param name="ChannelId"></param>
    public record Query(ulong AuthorId, ulong? GuildId, string Content, ulong ChannelId) : IRequest<QueryResponse<string>>;

    /// <inheritdoc />
    public async Task<QueryResponse<string>> Handle(Query request, CancellationToken cancellationToken)
    {
        string? response = await llmService.GetBeepskyChatResponseAsync(
                request.AuthorId,
                request.GuildId,
                request.Content,
                request.ChannelId,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(response))
        {
            response = "HAVE A SECURE DAY";
        }

        return response;
    }
}