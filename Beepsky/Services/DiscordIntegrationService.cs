using System.Collections.Concurrent;
using Beepsky.Core.Services;
using NetCord.Gateway;
using NetCord.Gateway.Voice;
using NetCord.Logging;
using NetCord.Rest;

namespace Beepsky.Services;

/// <inheritdoc />
public class DiscordIntegrationService(GatewayClient gatewayClient, RestClient restClient) : IDiscordIntegrationService
{
    private readonly ConcurrentDictionary<ulong, VoiceConnection> VoiceConnections = [];

    /// <inheritdoc />
    public async Task<Stream> GetOrCreateVoiceConnectionStreamAsync(ulong guildId, ulong voiceChannelId, CancellationToken cancellationToken = default)
    {
        if (!VoiceConnections.TryGetValue(guildId, out VoiceConnection? voiceConnection))
        {
            VoiceClient voiceClient = await gatewayClient.JoinVoiceChannelAsync(
                guildId,
                voiceChannelId,
                new VoiceClientConfiguration
                {
                    Logger = new BeepskyVoiceLogger(),
                },
                cancellationToken);


            voiceConnection = new VoiceConnection
            {
                VoiceClient = voiceClient
            };

            VoiceConnections[guildId] = voiceConnection;
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
            botVoiceState = await restClient.GetCurrentGuildUserVoiceStateAsync(guildId, cancellationToken: cancellationToken);
        }
        // This is thrown if the bot is not in a voice channel, so we can just ignore it
        catch (RestException)
        { }

        if (botVoiceState?.ChannelId != voiceChannelId)
        {
            await gatewayClient.UpdateVoiceStateAsync(new(guildId, voiceChannelId), cancellationToken: cancellationToken);
        }

        return voiceConnection.OpusEncodeStream;
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

    /// <inheritdoc />
    public async Task DisconnectFromGuildVoiceConnectionAsync(ulong guildId, CancellationToken cancellationToken = default)
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

    /// <inheritdoc />
    public async Task TriggerTypingStateInChannelAsync(ulong channelId, CancellationToken cancellationToken = default)
    {
        await restClient.TriggerTypingStateAsync(channelId, cancellationToken: cancellationToken);
    }
}

internal sealed class BeepskyVoiceLogger : IVoiceLogger
{
    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= LogLevel.Warning;
    }

    public void Log<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (exception is not null)
        {
            Serilog.Log.Error(exception, "Voice log [{LogLevel}]: {Message}", logLevel, formatter(state, exception));
        }
    }
}
