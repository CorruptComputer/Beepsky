using Beepsky.Core.Features.Statistics;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Beepsky.DiscordEventHandlers;

/// <summary>
///   Handles the MessageCreate event from NetCord
/// </summary>
/// <param name="sender"></param>
/// <param name="client"></param>
public sealed class GuildUserStatisticMessageCreateHandler(ISender sender, GatewayClient client) : IMessageCreateGatewayHandler
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

        // Direct mention
        if (arg.MentionedUsers.Any(u => u.Id == client.Token.Id))
        {
            isBeepskyChat = true;
        }

        if (arg.Guild is not null)
        {
            await sender.Send(new UpdateUserMessageCount.Command(arg.Author.Username, arg.Author.Id, arg.Guild.Name, arg.Guild.Id, isCommand, isBeepskyChat));
        }

        return;
    }
}
