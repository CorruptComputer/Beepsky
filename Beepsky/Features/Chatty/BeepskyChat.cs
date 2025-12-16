using Beepsky.Services;
using NetCord.Gateway;
using NetCord.Rest;

namespace Beepsky.Features.Chatty;

/// <inheritdoc />
public sealed partial class BeepskyChat(LLMService llmService, GatewayClient gatewayClient, RestClient restClient) : IRequestHandler<BeepskyChat.Command>
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

        if (request.Message.Channel is not null)
        {
            response = await llmService.GetBeepskyChatResponseAsync(
                request.Message.Author.Id,
                request.Message.GuildId,
                request.Message.Content.Replace($"<@{gatewayClient.Id}>", string.Empty).Trim(),
                request.Message.Channel,
                cancellationToken);
        }
        // DMs have the channel as null, even though DMChannel is a TextChannel -_-
        else
        {
            Channel channel = await restClient.GetChannelAsync(request.Message.ChannelId, cancellationToken: cancellationToken);
            if (channel is DMChannel dMChannel)
            {
                response = await llmService.GetBeepskyChatResponseAsync(
                    request.Message.Author.Id,
                    request.Message.GuildId,
                    request.Message.Content.Replace($"<@{gatewayClient.Id}>", string.Empty).Trim(),
                    dMChannel,
                    cancellationToken);
            }
        }

        if (string.IsNullOrWhiteSpace(response))
        {
            response = "HAVE A SECURE DAY";
        }

        await request.Message.ReplyAsync(response, cancellationToken: cancellationToken);

    }
}