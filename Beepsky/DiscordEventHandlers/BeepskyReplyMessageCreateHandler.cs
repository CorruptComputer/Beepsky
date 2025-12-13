using Beepsky.Features.Chatty;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

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
            await sender.Send(new BeepskyChat.Command(arg));
        }
        // Responses to normal messages
        else
        {
            await sender.Send(new NormalChat.Command(arg));

            // Test
            if (arg.Content.Contains("beepsky", StringComparison.OrdinalIgnoreCase))
            {
                await arg.AddReactionAsync(new("👋"));
            }
        }

        return;
    }
}
