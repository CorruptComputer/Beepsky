namespace Beepsky.Features.Chatty.Normal;

/// <inheritdoc />
public sealed class IsMessageWarhammerRelated : IRequestHandler<IsMessageWarhammerRelated.Command, QueryResponse<bool>>
{
    private readonly List<string> warhammerKeywords =
    [
        "imperium",
        "warhammer",
        "40k",
        "wh40k",
        "space marine",
        "astartes",
        "necron",
        "ork",
        "orks",
        "tyranid",
        "eldar",
        "aeldari",
        "games workshop",
        "chaos space",
        "primarch",
        "codex"
    ];

    /// <summary>
    ///   Handles a normal chat message, not a command or direct mention
    /// </summary>
    /// <param name="MessageContent"></param>
    public record Command(string MessageContent) : IRequest<QueryResponse<bool>>;

    /// <inheritdoc />
    public async Task<QueryResponse<bool>> Handle(Command request, CancellationToken cancellationToken)
    {
        foreach (string keyword in warhammerKeywords)
        {
            if (request.MessageContent.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}