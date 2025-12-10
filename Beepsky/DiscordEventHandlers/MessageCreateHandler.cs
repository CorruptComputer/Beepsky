using Beepsky.Features.Chatty;
using Beepsky.Features.Commands;
using Beepsky.Features.Commands.Statistics;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Beepsky.DiscordEventHandlers;

/// <summary>
///   Handles the MessageCreate event from NetCord
/// </summary>
/// <param name="sender"></param>
/// <param name="client"></param>
public sealed class MessageCreateHandler(ISender sender, GatewayClient client) : IMessageCreateGatewayHandler
{
    /// <inheritdoc />
    public async ValueTask HandleAsync(Message arg)
    {
        if (arg.Author.IsBot)
        {
            return;
        }

        bool isCommand = arg.Content.StartsWith(BeepskyConfiguration.Prefix);
        bool isBeepskyChat = false;

        // Direct mentions
        if (arg.MentionedUsers.Any(u => u.Id == client.Token.Id))
        {
            isBeepskyChat = true;
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

        if (arg.Guild is not null)
        {
            await sender.Send(new UpdateUserMessageCount.Command(arg.Author, arg.Guild, isCommand, isBeepskyChat));
        }

        return;
    }
}
