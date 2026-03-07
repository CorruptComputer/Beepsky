using Beepsky.Core.Features.Chatty;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace Beepsky.DiscordEventHandlers;

/// <summary>
///   Handles the MessageCreate event from NetCord
/// </summary>
/// <param name="sender"></param>
/// <param name="client"></param>
public sealed class BeepskyReplyMessageCreateHandler(ISender sender, GatewayClient client) : IMessageCreateGatewayHandler
{
    /// <inheritdoc />
    public async ValueTask HandleAsync(Message arg)
    {
        if (arg.Author.IsBot)
        {
            return;
        }

        // Direct mentions
        if (arg.MentionedUsers.Any(u => u.Id == client.Token.Id))
        {
            string? response = await sender.Send(new GetBeepskyChatResponse.Query(arg.Author.Id, arg.GuildId, arg.Content, arg.ChannelId));
            if (!string.IsNullOrWhiteSpace(response))
            {
                await arg.ReplyAsync(new ReplyMessageProperties()
                {
                    Content = response
                });
            }
        }
        // Responses to normal messages
        else
        {
            string? response = await sender.Send(new GetNormalChatResponse.Query(arg.Author.Id, arg.GuildId, arg.Content, arg.ChannelId));
            if (!string.IsNullOrWhiteSpace(response))
            {
                await arg.ReplyAsync(new ReplyMessageProperties()
                {
                    Content = response
                });
            }

            // Test
            if (arg.Content.Contains("beepsky", StringComparison.OrdinalIgnoreCase))
            {
                await arg.AddReactionAsync(new("👋"));
            }
        }

        return;
    }
}
