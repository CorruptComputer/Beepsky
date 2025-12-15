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
        "chaos marine",
        "primarch",
        "codex",
        "khorne",
        "nurgle",
        "tzeentch",
        "slaanesh",
        "world eater",
        "iron warrior",
        "raven guard",
        "blood angel",
        "ultramarine",
        "dark angel",
        "grey knight",
        "inquisition",
        "tau",
        "genestealer",
        "imperial guard",
        "adeptus mechanicus",
        "adeptus custodes",
        "sister of battle",
        "sisters of battle",
        "drukhari",
        "harlequin",
        "emperor's children",
        "emperors children",
    ];

    /// <summary>
    ///   Handles a normal chat message, not a command or direct mention
    /// </summary>
    /// <param name="MessageContent"></param>
    public record Command(string MessageContent) : IRequest<QueryResponse<bool>>;

    /// <inheritdoc />
    public Task<QueryResponse<bool>> Handle(Command request, CancellationToken cancellationToken)
    {
        QueryResponse<bool> response = false;

        foreach (string keyword in warhammerKeywords)
        {
            if (request.MessageContent.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                response = true;
                break;
            }
        }

        return Task.FromResult(response);
    }
}