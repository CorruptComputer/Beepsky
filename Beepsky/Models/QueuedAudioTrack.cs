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
    ///   The time this track was queued
    /// </summary>
    public DateTimeOffset QueuedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    ///   The current state of this queued audio track
    /// </summary>
    public required State CurrentState { get; set; }

    /// <summary>
    ///   The type of download for this track
    /// </summary>
    public required DownloadType Type { get; init; }

    /// <summary>
    ///   The URI of the track to download
    /// </summary>
    public required Uri TrackUri { get; init; }

    /// <summary>
    ///   The title of the track, if known
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    ///   The duration of the track, if known
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    ///   If downloaded, the file path of the downloaded audio file
    /// </summary>
    public string? DownloadedFilePath { get; set; }

    /// <summary>
    ///   The cancellation token source for this track
    /// </summary>
    public required CancellationTokenSource CancellationTokenSource { get; init; }

    /// <summary>
    ///   The current state of this queued audio track
    /// </summary>
    public enum State
    {
        /// <summary>
        ///   Freshly queued for download
        /// </summary>
        QueuedForDownload,

        /// <summary>
        ///   Is currently being downloaded
        /// </summary>
        Downloading,

        /// <summary>
        ///   Waiting for playback
        /// </summary>
        QueuedForPlayback,

        /// <summary>
        ///   Is currently being played
        /// </summary>
        Playing
    }

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

