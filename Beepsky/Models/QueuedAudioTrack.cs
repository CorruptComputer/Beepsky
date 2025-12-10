using NetCord.Gateway.Voice;

namespace Beepsky.Models;

/// <summary>
///   Represents an audio track queued for download or playback
/// </summary>
public sealed record QueuedAudioTrack
{
    /// <summary>
    ///   The guild ID this track is queued for
    /// </summary>
    public required ulong GuildId { get; init; }

    /// <summary>
    ///   The voice channel ID this track will be played in
    /// </summary>
    public required ulong VoiceChannelId { get; init; }

    /// <summary>
    ///   The type of download for this track
    /// </summary>
    public required DownloadType Type { get; init; }

    /// <summary>
    ///   The URI of the track to download
    /// </summary>
    public required Uri TrackUri { get; init; }

    /// <summary>
    ///   If downloaded, the file path of the downloaded audio file
    /// </summary>
    public string? DownloadedFilePath { get; set; }

    /// <summary>
    ///   The type of download for a queued audio track
    /// </summary>
    public enum DownloadType
    {
        /// <summary>
        ///   YouTube download
        /// </summary>
        YouTube
    }
}

