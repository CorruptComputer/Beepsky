using Beepsky.Services;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace Beepsky.DiscordEventHandlers;

/// <summary>
///   Handles the VoiceStateUpdate event from NetCord
/// </summary>
/// <param name="gatewayClient"></param>
/// <param name="restClient"></param>
/// <param name="audioQueueService"></param>
public class VoiceStateUpdateHandler(GatewayClient gatewayClient, RestClient restClient, AudioQueueService audioQueueService) : IVoiceStateUpdateGatewayHandler
{
    /// <inheritdoc />
    public async ValueTask HandleAsync(VoiceState arg)
    {
        // Happened to the bot
        if (arg.UserId == gatewayClient.Id)
        {
            if (arg.ChannelId is null)
            {
                audioQueueService.AddStopForGuild(arg.GuildId);
            }
        }
        // Someone else disconnected from voice
        else if (arg.ChannelId is null)
        {
            // Get the bot's voice state
            VoiceState? botVoiceState = null;
            try
            {
                botVoiceState = await restClient.GetCurrentGuildUserVoiceStateAsync(arg.GuildId);
            }
            // This is thrown if the bot is not in a voice channel, so we can just ignore it
            catch (RestException)
            { }

            if (botVoiceState?.ChannelId is not null)
            {
                IReadOnlyDictionary<ulong, VoiceState> guildVoiceStates = gatewayClient.Cache.Guilds[arg.GuildId].VoiceStates;
                // Check if there is anyone left in the channel
                bool anyoneLeft = guildVoiceStates.Values.Any(vs => vs.ChannelId == botVoiceState.ChannelId && vs.UserId != gatewayClient.Id);
                if (!anyoneLeft)
                {
                    audioQueueService.AddStopForGuild(arg.GuildId);
                }
            }
        }
    }
}
