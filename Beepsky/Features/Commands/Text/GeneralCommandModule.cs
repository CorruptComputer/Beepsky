using Beepsky.Exceptions;
using NetCord.Gateway;
using NetCord.Services.Commands;

namespace Beepsky.Features.Commands.Text;

/// <inheritdoc />
public sealed class GeneralCommandModule : CommandModule<CommandContext>
{
    /// <summary>
    ///   8ball command
    /// </summary>
    /// <param name="question"></param>
    /// <returns></returns>
    /// <exception cref="BeepskyException"></exception>
    [Command("8ball")]
    public static string EightBall([CommandParameter(Remainder = true)] string? question = null)
    {
        int certainty = _random.Next(EightBallResponses.Count * 100) % EightBallResponses.Count;

        List<string> possibleResponses = EightBallResponses[certainty];
        int responseIndex = _random.Next(possibleResponses.Count * 100) % possibleResponses.Count;
        string response = possibleResponses[responseIndex];

        string certaintyEmoji = certainty switch
        {
            0 => "👍",
            1 => "🤔",
            2 => "👎",
            _ => throw new BeepskyException("Unexpected certainty value in EightBall feature.")
        };

        string message = string.Empty;

        if (!string.IsNullOrWhiteSpace(question))
        {
            message += $"\"{question}\" ";
        }

        message += $"🎱->{certaintyEmoji} `{response}`";

        return message;
    }

    /// <summary>
    ///   Help command
    /// </summary>
    /// <returns></returns>
    [Command("help")]
    public static string Help()
    {
        string helpMessage =
@$"**Beepsky**
- {BeepskyConfiguration.Prefix}8ball [question (optional)] - Ask the magic 8-ball a question
- {BeepskyConfiguration.Prefix}help - Show this help message
- {BeepskyConfiguration.Prefix}ping - Pong!
- {BeepskyConfiguration.Prefix}yt [link] - Play a YouTube link in your current voice channel (guild only)
";

        return helpMessage;
    }

    /// <summary>
    ///   Ping command
    /// </summary>
    /// <returns></returns>
    [Command("ping")]
    public static string Ping() => "Pong!";

    private static readonly Random _random = new();

    /*
      Most of these are generic 8ball quotes, but some are references
      answers[0][x] = Positive
      answers[1][x] = Unsure
      answers[2][x] = Negative
    */
    private static readonly List<List<string>> EightBallResponses =
    [
        [
            "It is certain",
            "It is decidedly so",
            "Without a doubt",
            "Yes definitely",
            "You may rely on it",
            "As I see it, yes",
            "Most likely",
            "Outlook good",
            "Yes",
            "Signs point to yes",
            "Have a secure day"
        ],
        [
            "Reply hazy try again",
            "Ask again later",
            "Better not tell you now",
            "Cannot predict now",
            "Concentrate and ask again",
            "I am the law",
            "God made tomorrow for the crooks we don't catch today",
            "You can't out run a radio"
        ],
        [
            "Don't count on it",
            "My reply is no",
            "My sources say no",
            "Outlook not so good",
            "Very doubtful",
            "Criminal detected",
            "Prepare for justice",
            "Freeze scumbag"
        ]
    ];
}
