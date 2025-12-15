using System.Collections.Concurrent;
using Beepsky.Exceptions;
using Serilog;

namespace Beepsky.Services;

/// <summary>
///   Handles audio playback functionality
///   This should be a singleton that can be used by any thread
/// </summary>
public class AudioQueueService
{
    // <QueuedAudioTrackId, QueuedAudioTrack>
    // ID is just needed to make it possible to remove specific tracks, ConcurrentBag and ConcurrentQueue don't support removal of specific items
    private readonly ConcurrentDictionary<Guid, QueuedAudioTrack> TrackQueue = [];

    // <GuildId, ShouldSkipCurrentlyPlayingTrack>
    private readonly ConcurrentDictionary<ulong, bool> GuildSkips = [];

    /// <summary>
    ///   Adds a track to the download queue, once downloaded it will be moved to the playback queue
    /// </summary>
    /// <param name="voiceChannelId"></param>
    /// <param name="guildId"></param>
    /// <param name="track"></param>
    /// <returns></returns>
    public bool AddTrackToQueue(ulong voiceChannelId, ulong guildId, string track)
    {
        bool valid = Uri.TryCreate(track, UriKind.Absolute, out Uri? result);

        if (!valid
            || result is null
            || !result.IsWellFormedOriginalString())
        {
            Log.Warning("Invalid track URL provided: {Link}", track);
            return false;
        }

        if (result.Host is "www.youtube.com" or "youtube.com" or "youtu.be")
        {
            if (result.Host is "youtu.be")
            {
                result = new UriBuilder("https", "www.youtube.com")
                {
                    Path = "/watch",
                    Query = $"v={result.AbsolutePath.TrimStart('/')}"
                }.Uri;
            }

            Guid trackId = Guid.NewGuid();
            // Guard against random chance fuckery
            while (TrackQueue.ContainsKey(trackId))
            {
                trackId = Guid.NewGuid();
            }

            TrackQueue[trackId] = new()
            {
                GuildId = guildId,
                VoiceChannelId = voiceChannelId,
                CurrentState = QueuedAudioTrack.State.QueuedForDownload,
                Type = QueuedAudioTrack.DownloadType.YouTube,
                TrackUri = result,
                CancellationTokenSource = new()
            };

            return true;
        }

        Log.Warning("Unsupported track URL provided: {Link}", track);
        return false;
    }

    /// <summary>
    ///   Adds a skip request for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public bool AddSkipForGuild(ulong guildId)
    {
        return GuildSkips.AddOrUpdate(guildId, true, (_, _) => true);
    }

    /// <summary>
    ///   Adds a stop request for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public bool AddStopForGuild(ulong guildId)
    {
        // Clear the current queue for the guild
        IEnumerable<QueuedAudioTrack> tracks = TrackQueue.Values.Where(track => track.GuildId == guildId);
        foreach (QueuedAudioTrack track in tracks)
        {
            track.CurrentState = QueuedAudioTrack.State.Cancelled;
            try
            {
                track.CancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException) { /* Ignore */ }
        }

        // Finally need to skip the currently playing track
        return GuildSkips.AddOrUpdate(guildId, true, (_, _) => true);
    }

    /// <summary>
    ///   Gets a list of guilds that have queues
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ulong> GetGuildsWithPlaybackQueues()
    {
        return TrackQueue.Select(track => track.Value.GuildId).Distinct();
    }

    /// <summary>
    ///   Gets the full queue for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public IEnumerable<QueuedAudioTrack> GetQueueForGuild(ulong guildId)
    {
        return TrackQueue.Where(track => track.Value.GuildId == guildId && track.Value.CurrentState != QueuedAudioTrack.State.Playing).Select(tp => tp.Value);
    }

    /// <summary>
    ///   Gets a list of guilds that have skip requests
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ulong> GetGuildsWithSkips()
    {
        return GuildSkips.Where(kvp => kvp.Value).Select(kvp => kvp.Key);
    }

    /// <summary>
    ///   Pops the next track from the queue for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public QueuedAudioTrack? GetNextPlayback(ulong guildId)
    {
        IEnumerable<KeyValuePair<Guid, QueuedAudioTrack>> guildTracksWaitingForPlayback = TrackQueue.Where(track => track.Value.GuildId == guildId && track.Value.CurrentState == QueuedAudioTrack.State.QueuedForPlayback);
        KeyValuePair<Guid, QueuedAudioTrack>? nextTrack = guildTracksWaitingForPlayback.OrderBy(track => track.Value.QueuedAt).FirstOrDefault();

        return nextTrack?.Value;
    }

    /// <summary>
    ///   Gets the currently playing track for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    /// <exception cref="BeepskyException"></exception>
    public QueuedAudioTrack? GetCurrentlyPlayingTrackForGuild(ulong guildId)
    {
        IEnumerable<KeyValuePair<Guid, QueuedAudioTrack>> guildTracksPlaying = TrackQueue.Where(track => track.Value.GuildId == guildId && track.Value.CurrentState == QueuedAudioTrack.State.Playing);

        if (guildTracksPlaying.Count() > 1)
        {
            throw new BeepskyException("Invalid state, multiple tracks playing for guild " + guildId);
        }

        return guildTracksPlaying.Select(tp => tp.Value).FirstOrDefault();
    }

    /// <summary>
    ///   Get the next track to download from the queue
    /// </summary>
    /// <returns></returns>
    public QueuedAudioTrack? GetNextDownload()
    {
        IEnumerable<KeyValuePair<Guid, QueuedAudioTrack>> guildTracksWaitingForDownload = TrackQueue.Where(track => track.Value.CurrentState == QueuedAudioTrack.State.QueuedForDownload);
        KeyValuePair<Guid, QueuedAudioTrack>? nextTrack = guildTracksWaitingForDownload.OrderBy(track => track.Value.QueuedAt).FirstOrDefault();

        return nextTrack?.Value;
    }

    /// <summary>
    ///   Clears the skip request for a guild
    /// </summary>
    /// <param name="guildId"></param>
    public void ClearSkipForGuild(ulong guildId)
    {
        GuildSkips.AddOrUpdate(guildId, false, (_, _) => false);
    }

    /// <summary>
    ///   Removes a specific track from the queue
    /// </summary>
    /// <param name="track"></param>
    public void RemoveTrack(QueuedAudioTrack track)
    {
        KeyValuePair<Guid, QueuedAudioTrack>? trackToRemove = TrackQueue.FirstOrDefault(kvp => kvp.Value == track);
        if (trackToRemove is not null)
        {
            TrackQueue.TryRemove(trackToRemove.Value.Key, out _);
        }
    }
}