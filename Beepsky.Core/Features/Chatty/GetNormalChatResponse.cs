using Beepsky.Core.Features.Chatty.Normal;

namespace Beepsky.Core.Features.Chatty;

/// <inheritdoc />
public sealed class GetNormalChatResponse(ISender sender) : IRequestHandler<GetNormalChatResponse.Query, QueryResponse<string>>
{
    /// <summary>
    ///   Handles a normal chat message, not a command or direct mention
    /// </summary>
    /// <param name="AuthorId"></param>
    /// <param name="GuildId"></param>
    /// <param name="Content"></param>
    /// <param name="ChannelId"></param>
    public record Query(ulong AuthorId, ulong? GuildId, string Content, ulong ChannelId) : IRequest<QueryResponse<string>>;

    /// <inheritdoc />
    public async Task<QueryResponse<string>> Handle(Query request, CancellationToken cancellationToken)
    {
        // These kinda suck to read, but too bad! I need a way to test this
#if DEBUG
        if (request.AuthorId == (ulong)WellKnownUsers.Monke)
#else
        if (request.AuthorId == (ulong)WellKnownUsers.Skeleton)
#endif
        {
            // Tell skeleton to go back to the warhammer channel if he talks about it outside there, 1 in 20 chance
            bool isWarhammerRelated = await sender.Send(new IsMessageWarhammerRelated.Command(request.Content), cancellationToken);

            if (isWarhammerRelated
                && request.ChannelId != (ulong)WellKnownChannels.Warhammer)
            {
                // Roll a d20
                int roll =
#if DEBUG
                20; // Gaurenteed, absolutely random. I promise!
#else
                Random.Shared.Next(20) + 1;
#endif
                // Oh yeah, its show time
                if (roll == 20)
                {
                    return await sender.Send(new GetGoBackToWarhammerChatResponse.Query(request.Content), cancellationToken);
                }
            }

            // 1 in 1,000,000 to respond to any message of his with something?
        }
        else if (request.AuthorId == (ulong)WellKnownUsers.Monke)
        {
            if (request.Content.Contains("banana", StringComparison.OrdinalIgnoreCase))
            {
                return "🍌";
            }
        }

        return (string?)null;
    }
}