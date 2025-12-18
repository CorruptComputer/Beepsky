using NetCord.Gateway.Voice;

namespace Beepsky.Models;

/// <summary>
///   Represents a voice connection in a guild
/// </summary>
public sealed record VoiceConnection
{
    /// <summary>
    ///   The voice client for this connection
    /// </summary>
    public required VoiceClient VoiceClient { get; init; }

    /// <summary>
    ///   The output stream for this connection, either <ref cref="VoiceOutStream"/> or <ref cref="SpeedNormalizingStream"/>
    ///   See: https://github.com/NetCordDev/NetCord/blob/alpha/NetCord/Gateway/Voice/VoiceClient.cs#L537C5-L537C65
    /// </summary>
    public Stream? OutStream { get; set; }

    /// <summary>
    ///   The Opus encode stream for this connection
    /// </summary>
    public OpusEncodeStream? OpusEncodeStream { get; set; }
}
