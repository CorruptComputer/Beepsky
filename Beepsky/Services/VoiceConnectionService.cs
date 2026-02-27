using System.Collections.Concurrent;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Rest;

namespace Beepsky.Services;

/// <summary>
///   Service that handles voice connections for guilds
///   This should be a singleton that can be used by any thread
/// </summary>
/// <param name="gatewayClient"></param>
/// <param name="restClient"></param>
public class VoiceConnectionService(GatewayClient gatewayClient, RestClient restClient)
{
    private readonly ConcurrentDictionary<ulong, VoiceConnection> VoiceConnections = [];

    /// <summary>
    ///   Gets or creates a voice connection for the given track
    /// </summary>
    /// <param name="track"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<VoiceConnection> GetOrCreateVoiceConnectionForTrackAsync(QueuedAudioTrack track, CancellationToken cancellationToken = default)
    {
        if (!VoiceConnections.TryGetValue(track.GuildId, out VoiceConnection? voiceConnection))
        {
            VoiceClient voiceClient = await gatewayClient.JoinVoiceChannelAsync(
                track.GuildId,
                track.VoiceChannelId,
                new VoiceClientConfiguration
                {
                    Logger = new BeepskyVoiceLogger(),
                },
                cancellationToken);


            voiceConnection = new VoiceConnection
            {
                VoiceClient = voiceClient
            };

            VoiceConnections[track.GuildId] = voiceConnection;
        }

        if (voiceConnection.OpusEncodeStream is null || voiceConnection.OutStream is null)
        {
            await voiceConnection.VoiceClient.StartAsync(cancellationToken);
            await voiceConnection.VoiceClient.EnterSpeakingStateAsync(new SpeakingProperties(SpeakingFlags.Microphone), cancellationToken: cancellationToken);
            voiceConnection.OutStream = voiceConnection.VoiceClient.CreateVoiceStream();
            voiceConnection.OpusEncodeStream = new(voiceConnection.OutStream, PcmFormat.Short, VoiceChannels.Stereo, OpusApplication.Audio);
        }

        VoiceState? botVoiceState = null;
        try
        {
            botVoiceState = await restClient.GetCurrentGuildUserVoiceStateAsync(track.GuildId, cancellationToken: cancellationToken);
        }
        // This is thrown if the bot is not in a voice channel, so we can just ignore it
        catch (RestException)
        { }

        if (botVoiceState?.ChannelId != track.VoiceChannelId)
        {
            await gatewayClient.UpdateVoiceStateAsync(new(track.GuildId, track.VoiceChannelId), cancellationToken: cancellationToken);
        }

        return voiceConnection;
    }

    /// <summary>
    ///   Gets the voice connection for the given guild, or null if none exists
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public VoiceConnection? GetVoiceConnectionForGuild(ulong guildId)
    {
        VoiceConnections.TryGetValue(guildId, out VoiceConnection? voiceConnection);

        return voiceConnection;
    }

    /// <summary>
    ///   Disconnects from the voice channel in the given guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task DisconnectFromGuildAsync(ulong guildId, CancellationToken cancellationToken = default)
    {
        await gatewayClient.UpdateVoiceStateAsync(new(guildId, null), cancellationToken: cancellationToken);

        if (VoiceConnections.TryRemove(guildId, out VoiceConnection? voiceConnection))
        {
            if (voiceConnection.OpusEncodeStream is not null)
            {
                await voiceConnection.OpusEncodeStream.FlushAsync(cancellationToken);
                voiceConnection.OpusEncodeStream.Dispose();
                voiceConnection.OpusEncodeStream = null;
            }

            if (voiceConnection.OutStream is not null)
            {
                await voiceConnection.OutStream.FlushAsync(cancellationToken);
                voiceConnection.OutStream.Dispose();
                voiceConnection.OutStream = null;
            }
        }
    }
}
