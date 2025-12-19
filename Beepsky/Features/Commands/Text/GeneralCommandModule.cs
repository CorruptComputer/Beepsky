using System.Globalization;
using Beepsky.Exceptions;
using Beepsky.Features.TerribleCounting;
using Beepsky.Models.MathExpressions;
using NetCord.Services.Commands;

namespace Beepsky.Features.Commands.Text;

/// <inheritdoc />
public sealed class GeneralCommandModule(ISender sender) : CommandModule<CommandContext>
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
        int certainty = Random.Shared.Next(EightBallResponses.Length);
        string[] possibleResponses = EightBallResponses[certainty];
        string response = Random.Shared.GetItems(possibleResponses, 1).First();

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
    ///   Generates an equation that equals the given number
    /// </summary>
    /// <param name="equals"></param>
    /// <returns></returns>
    [Command("eq")]
    public async Task<string> Equation([CommandParameter(Remainder = true)] int equals)
    {
        Expression? expression = await sender.Send(new GenerateExpressionTree.Query(equals));

        return expression?.ToString() ?? "Could not generate expression.";
    }

    /// <summary>
    ///   Generates an equation that equals the given number
    /// </summary>
    /// <param name="expressionStr"></param>
    /// <returns></returns>
    [Command("eval")]
    public static Task<string> Evaluate([CommandParameter(Remainder = true)] string expressionStr)
    {
        if (!Expression.TryParse(expressionStr, out Expression? expression))
        {
            return Task.FromResult("Invalid expression format.");
        }

        return Task.FromResult(Math.Round(expression.Root.Value, 10).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    ///   Help command
    /// </summary>
    /// <returns></returns>
    [Command("help")]
    public static string Help()
    {
        string helpMessage = $"""
            **Audio Commands** (server-only)
            - {BeepskyConfiguration.Prefix}q [link] - Queues a track to play from YouTube
            - {BeepskyConfiguration.Prefix}skip - Skip the currently playing track
            - {BeepskyConfiguration.Prefix}stop - Stop playback and clear the queue
            - {BeepskyConfiguration.Prefix}lq - Lists the current queue of tracks
            **General Commands**
            - {BeepskyConfiguration.Prefix}8ball [question (optional)] - Ask the magic 8-ball a question
            - {BeepskyConfiguration.Prefix}help - Show this help message
            - {BeepskyConfiguration.Prefix}eq [number] - Generate a random equation that equals the given number
            - {BeepskyConfiguration.Prefix}eval [expression] - Evaluate a mathematical expression
            - {BeepskyConfiguration.Prefix}ping - Pong!

            If you @ ping me, I might respond!
            """;

        return helpMessage;
    }

    /// <summary>
    ///   Ping command
    /// </summary>
    /// <returns></returns>
    [Command("ping")]
    public static string Ping() => "Pong!";

    /*
      Most of these are generic 8ball quotes, but some are references
      answers[0][x] = Positive
      answers[1][x] = Unsure
      answers[2][x] = Negative
    */
    private static readonly string[][] EightBallResponses =
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
