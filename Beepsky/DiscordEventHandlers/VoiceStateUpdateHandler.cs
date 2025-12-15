using Beepsky.Features.Jolly;
using Beepsky.Services;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace Beepsky.DiscordEventHandlers;

/// <summary>
///   Handles the VoiceStateUpdate event from NetCord
/// </summary>
/// <param name="gatewayClient"></param>
/// <param name="audioQueueService"></param>
/// <param name="sender"></param>
public class VoiceStateUpdateHandler(GatewayClient gatewayClient, AudioQueueService audioQueueService, ISender sender) : IVoiceStateUpdateGatewayHandler
{
    /// <inheritdoc />
    public async ValueTask HandleAsync(VoiceState arg)
    {
        // The bot was disconnected from vc
        if (arg.UserId == gatewayClient.Id)
        {
            // This is seperate from the above if intentionally, so other self-events are just silently ignored.
            if (arg.ChannelId is null)
            {
                audioQueueService.AddStopForGuild(arg.GuildId);
            }

            return;
        }

        // Get the servers voice states
        IReadOnlyDictionary<ulong, VoiceState> allGuildVoiceStates = gatewayClient.Cache.Guilds[arg.GuildId].VoiceStates;
        Dictionary<ulong, VoiceState> changedChannelVoiceStates = allGuildVoiceStates.Where(vs => vs.Value.ChannelId == arg.ChannelId).ToDictionary();
        allGuildVoiceStates.TryGetValue(gatewayClient.Id, out VoiceState? botVoiceState);

        // Someone else disconnected from voice
        if (arg.ChannelId is null)
        {
            if (botVoiceState?.ChannelId is not null)
            {
                IEnumerable<VoiceState> usersInBotChannel = allGuildVoiceStates.Values.Where(vs => vs.ChannelId == botVoiceState.ChannelId
                                                                                                && vs.UserId != gatewayClient.Id);
                // Check if there is anyone left in the channel, depending on timing the user that just left may or my not be in the cache still
                bool anyoneLeft = usersInBotChannel.Any(vs => vs.UserId != arg.UserId);
                if (!anyoneLeft)
                {
                    audioQueueService.AddStopForGuild(arg.GuildId);
                }
            }

            return;
        }

        // Someone joined voice, channel is otherwise empty.
        // The bot is not connected to voice.
        // And the month is December
        if (botVoiceState is null
            && changedChannelVoiceStates.Count == 0 // Count not updated in cache yet
            && DateTime.UtcNow.Month == 12)
        {
            await sender.Send(new QueueRandomChristmasSongs.Command(arg.GuildId, arg.ChannelId.Value));
        }
    }
}
