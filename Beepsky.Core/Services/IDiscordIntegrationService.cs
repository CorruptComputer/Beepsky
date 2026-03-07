namespace Beepsky.Core.Services;

/// <summary>
///   Service interface for Discord integrated operations.
/// </summary>
public interface IDiscordIntegrationService
{
    /// <summary>
    ///   Gets or creates a voice connection stream for a given guild and voice channel.
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="voiceChannelId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Stream> GetOrCreateVoiceConnectionStreamAsync(ulong guildId, ulong voiceChannelId, CancellationToken cancellationToken = default);

    /// <summary>
    ///   Disconnects from the voice channel in the given guild.
    /// </summary>
    /// <param name="guildId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task DisconnectFromGuildVoiceConnectionAsync(ulong guildId, CancellationToken cancellationToken = default);

    /// <summary>
    ///   Triggers the typing state in a given channel.
    /// </summary>
    /// <param name="channelId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task TriggerTypingStateInChannelAsync(ulong channelId, CancellationToken cancellationToken = default);
}
