using System.Text.RegularExpressions;
using LLama;
using LLama.Common;
using NetCord.Gateway;
using NetCord.Rest;

namespace Beepsky.Features.Chatty;

/// <inheritdoc />
public sealed partial class BeepskyChat(BeepskyConfiguration config, GatewayClient gatewayClient) : IRequestHandler<BeepskyChat.Command>
{
    private const string defaultPromptText = """
        You are Beepsky, a bureaucratic policing robot in an overly opressive city.
        You should respond in robo-cop style authoritarian and overly bureaucratic mannors.
        Keep your answers concise and to the point and you should ALWAYS speak in CAPITAL LETTERS.
        Add a new line once you have finished your response.

        When using harsher, enforcement-like, language you may make up random infractions with article numbers or bureaucratic sounding names.
        Part of your job is interacting with and holding conversations with citizens about whatever is on their mind.
        """;

    private const string criminalDetectedPromptText = """
        The user has been identified as a known criminal by your internal database.
        You must treat them with extra harshness and enforce the law to the fullest extent.
        """;

    /// <summary>
    ///   Handles a direct mention of Beepsky
    /// </summary>
    /// <param name="Message"></param>
    public record Command(Message Message) : IRequest;

    /// <inheritdoc />
    public async Task Handle(Command request, CancellationToken cancellationToken)
    {
        bool criminalDetected = false;
        if (request.Message.Author.Id is (ulong)WellKnownUsers.Skeleton
                                      or (ulong)WellKnownUsers.Svally)
        {
            criminalDetected = true;
        }

        string? response = null;
        if (config.LLamaModel is not null
            && File.Exists(config.LLamaModel))
        {
            DateTimeOffset lastTypingTime = DateTimeOffset.UtcNow;
            if (request.Message.Channel is not null)
            {
                await request.Message.Channel.TriggerTypingStateAsync(cancellationToken: cancellationToken);
            }

            ModelParams parameters = new(config.LLamaModel)
            {
                ContextSize = 2048, // The longest length of chat as memory.
            };
            using LLamaWeights model = LLamaWeights.LoadFromFile(parameters);
            using LLamaContext context = model.CreateContext(parameters);
            InteractiveExecutor executor = new(context);

            ChatHistory chatHistory = new([
                new ChatHistory.Message(AuthorRole.System, defaultPromptText),
            ]);

            if (criminalDetected)
            {
                chatHistory.Messages.Add(new ChatHistory.Message(AuthorRole.System, criminalDetectedPromptText));
            }

            ChatSession session = new(executor, chatHistory);

            InferenceParams inferenceParams = new()
            {
                MaxTokens = 128,
                AntiPrompts = ["User:", ".\n", "?\n", "!\n"],
            };

            List<string> responses = [];
            // Remove the mention from the message
            string userPromptStr = request.Message.Content.Replace($"<@{gatewayClient.Id}>", string.Empty).Trim();
            await foreach (string text in session.ChatAsync(new ChatHistory.Message(AuthorRole.User, userPromptStr), inferenceParams, cancellationToken))
            {
                responses.Add(text);

                if (request.Message.Channel is not null
                    && (DateTimeOffset.UtcNow - lastTypingTime).TotalSeconds >= 5)
                {
                    lastTypingTime = DateTimeOffset.UtcNow;
                    await request.Message.Channel.TriggerTypingStateAsync(cancellationToken: cancellationToken);
                }
            }

            response = chatHistory.Messages.FirstOrDefault(m => m.AuthorRole == AuthorRole.Assistant)?.Content;

            Regex allUpperCaseSentences = FindAllUpperCaseSentencesRegex();
            MatchCollection matches = allUpperCaseSentences.Matches(response ?? string.Empty);
            if (matches.Count > 0)
            {
                response = matches.FirstOrDefault()?.Value;
            }
            else
            {
                response = null;
            }

            response = response?.Trim();
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            response = "HAVE A SECURE DAY";
        }

        await request.Message.ReplyAsync(response, cancellationToken: cancellationToken);

    }

    // I hate regex so much, who ever came up with this is absolutely deranged
    [GeneratedRegex(@"(?:[A-Z][A-Z0-9[ \t\.\-\,\#\:\']*(?:<#\d+>)?[A-Z0-9[ \t\.\-\,\#\:\']*[.?!])+", RegexOptions.Compiled)]
    private static partial Regex FindAllUpperCaseSentencesRegex();
}