namespace Beepsky.Core.Features.Chatty.Normal;

/// <inheritdoc />
public sealed class IsMessageWarhammerRelated : IRequestHandler<IsMessageWarhammerRelated.Command, QueryResponse<bool>>
{
    private readonly List<string> warhammerKeywords =
    [
        "40k",
        "adeptus custodes",
        "adeptus mechanicus",
        "aeldari",
        "astartes",
        "black templar",
        "blood angel",
        "chaos marine",
        "codex",
        "dark angel",
        "death guard",
        "drukhari",
        "eldar",
        "emperor's children",
        "emperors children",
        "games workshop",
        "genestealer",
        "grey knight",
        "harlequin",
        "imperial guard",
        "imperium",
        "inquisition",
        "iron hands",
        "iron warrior",
        "khorne",
        "necron",
        "nurgle",
        "ork",
        "orks",
        "primarch",
        "raven guard",
        "salamanders",
        "sister of battle",
        "sisters of battle",
        "slaanesh",
        "space marine",
        "space wolves",
        "tau",
        "tyranid",
        "tzeentch",
        "ultramarine",
        "warhammer",
        "wh40k",
        "white scars",
        "world eater"
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