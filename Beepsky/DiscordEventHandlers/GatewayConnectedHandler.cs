using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Beepsky.DiscordEventHandlers;

/// <summary>
///   Handles the MessageCreate event from NetCord
/// </summary>
/// <param name="client"></param>
public sealed class GatewayConnectedHandler(GatewayClient client) : IConnectGatewayHandler
{
    /// <inheritdoc />
    public async ValueTask HandleAsync()
    {
        PresenceProperties presenceProperties = new(UserStatusType.Online)
        {
            Activities = [
                new(";help", UserActivityType.Watching)
            ]
        };

        await client.UpdatePresenceAsync(presenceProperties);
    }
}
