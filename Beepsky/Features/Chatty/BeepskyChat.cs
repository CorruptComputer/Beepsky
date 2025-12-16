using Beepsky.Services;
using NetCord.Gateway;

namespace Beepsky.Features.Chatty;

/// <inheritdoc />
public sealed partial class BeepskyChat(LLMService llmService, GatewayClient gatewayClient) : IRequestHandler<BeepskyChat.Command>
{

    /// <summary>
    ///   Handles a direct mention of Beepsky
    /// </summary>
    /// <param name="Message"></param>
    public record Command(Message Message) : IRequest;

    /// <inheritdoc />
    public async Task Handle(Command request, CancellationToken cancellationToken)
    {
        string? response = null;

        if (request.Message.Channel is TextChannel textChannel)
        {
            response = await llmService.GetBeepskyChatResponseAsync(
            request.Message.Author.Id,
            request.Message.GuildId,
            request.Message.Content.Replace($"<@{gatewayClient.Id}>", string.Empty).Trim(),
            textChannel,
            cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            response = "HAVE A SECURE DAY";
        }

        await request.Message.ReplyAsync(response, cancellationToken: cancellationToken);

    }
}