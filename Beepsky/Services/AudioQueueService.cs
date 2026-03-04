using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Web;
using Beepsky.Exceptions;
using Serilog;

// NetCord has a Channel type that we need to disambiguate here
using STC = System.Threading.Channels;

namespace Beepsky.Services;

/// <summary>
///   Handles audio playback functionality
///   This should be a singleton that can be used by any thread
/// </summary>
public class AudioQueueService : IDisposable
{
    private readonly STC.Channel<QueuedAudioTrack> _downloadChannel = STC.Channel.CreateUnbounded<QueuedAudioTrack>();
    private readonly SemaphoreSlim _playbackWakeSignal = new(0);

    /// <summary>
    ///   The channel reader for the download queue; consumed by <see cref="AudioDownloadService"/>
    /// </summary>
    public STC.ChannelReader<QueuedAudioTrack> DownloadChannelReader => _downloadChannel.Reader;

    /// <summary>
    ///   Wake signal for the playback loop; await this instead of polling
    /// </summary>
    public SemaphoreSlim PlaybackWakeSignal => _playbackWakeSignal;

    /// <summary>
    ///   Releases the playback wake signal so <see cref="AudioPlaybackService"/> processes the next state change
    /// </summary>
    public void SignalPlaybackReady() => _playbackWakeSignal.Release();

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

            // Remove all query parameters from result except for "v="
            NameValueCollection query = HttpUtility.ParseQueryString(result.Query);
            string? v = query["v"];
            result = new UriBuilder(result)
            {
                Query = $"v={(string.IsNullOrEmpty(v) ? "dQw4w9WgXcQ" : v)}"
            }.Uri;

            Guid trackId = Guid.NewGuid();
            // Guard against random chance fuckery
            while (TrackQueue.ContainsKey(trackId))
            {
                // If it happens a second time I just give up
                trackId = Guid.NewGuid();
            }

            QueuedAudioTrack newTrack = new()
            {
                GuildId = guildId,
                VoiceChannelId = voiceChannelId,
                CurrentState = QueuedAudioTrack.State.QueuedForDownload,
                Type = QueuedAudioTrack.DownloadType.YouTube,
                TrackUri = result,
                CancellationTokenSource = new()
            };

            TrackQueue[trackId] = newTrack;
            _downloadChannel.Writer.TryWrite(newTrack);

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
        bool result = GuildSkips.AddOrUpdate(guildId, true, (_, _) => true);
        _playbackWakeSignal.Release();
        return result;
    }

    /// <summary>
    ///   Adds a stop request for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public bool AddStopForGuild(ulong guildId)
    {
        // Clear the current queue for the guild
        List<QueuedAudioTrack> tracks = [.. TrackQueue.Values.Where(track => track.GuildId == guildId)];
        foreach (QueuedAudioTrack track in tracks)
        {
            try
            {
                track.CancellationTokenSource.Cancel();
            }
            catch (ObjectDisposedException) { /* Ignore */ }
        }

        // Finally need to skip the currently playing track
        bool result = GuildSkips.AddOrUpdate(guildId, true, (_, _) => true);
        _playbackWakeSignal.Release();
        return result;
    }

    /// <summary>
    ///   Gets a list of guilds that have queues
    /// </summary>
    /// <returns></returns>
    public List<ulong> GetGuildsWithPlaybackQueues()
    {
        return [.. TrackQueue.Values.Select(track => track.GuildId).Distinct()];
    }

    /// <summary>
    ///   Gets the full queue for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public List<QueuedAudioTrack> GetQueueForGuild(ulong guildId)
    {
        return [.. TrackQueue.Values.Where(track => track.GuildId == guildId && track.CurrentState != QueuedAudioTrack.State.Playing)];
    }

    /// <summary>
    ///   Gets a list of guilds that have skip requests
    /// </summary>
    /// <returns></returns>
    public List<ulong> GetGuildsWithSkips()
    {
        return [.. GuildSkips.Where(kvp => kvp.Value).Select(kvp => kvp.Key)];
    }

    /// <summary>
    ///   Pops the next track from the queue for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public QueuedAudioTrack? GetNextPlayback(ulong guildId)
    {
        List<QueuedAudioTrack> guildTracksWaitingForPlayback = [.. TrackQueue.Values.Where(track => track.GuildId == guildId && track.CurrentState == QueuedAudioTrack.State.QueuedForPlayback)];
        List<QueuedAudioTrack> cancelledTracks = [.. guildTracksWaitingForPlayback.Where(track => track.CancellationTokenSource.IsCancellationRequested)];
        if (cancelledTracks.Count > 0)
        {
            Log.Information("Removing {Count} cancelled tracks from playback queue for guild {GuildId}", cancelledTracks.Count, guildId);
            foreach (QueuedAudioTrack cancelledTrack in cancelledTracks)
            {
                RemoveTrack(cancelledTrack);
                guildTracksWaitingForPlayback.Remove(cancelledTrack);
            }
        }

        return guildTracksWaitingForPlayback.OrderBy(track => track.QueuedAt).FirstOrDefault();
    }

    /// <summary>
    ///   Gets the currently playing track for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    /// <exception cref="BeepskyException"></exception>
    public QueuedAudioTrack? GetCurrentlyPlayingTrackForGuild(ulong guildId)
    {
        List<QueuedAudioTrack> guildTracksPlaying = [.. TrackQueue.Values.Where(track => track.GuildId == guildId && track.CurrentState == QueuedAudioTrack.State.Playing)];

        if (guildTracksPlaying.Count > 1)
        {
            throw new BeepskyException("Invalid state, multiple tracks playing for guild " + guildId);
        }

        return guildTracksPlaying.FirstOrDefault();
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
    ///   Resets a track's state to QueuedForDownload and re-writes it to the download channel
    /// </summary>
    /// <param name="track"></param>
    public void RequeueTrackForDownload(QueuedAudioTrack track)
    {
        track.CancellationTokenSource.TryReset();
        track.CurrentState = QueuedAudioTrack.State.QueuedForDownload;
        _downloadChannel.Writer.TryWrite(track);
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

        try
        {
            // Might as well go ahead and cancel it too
            track.CancellationTokenSource.Cancel();
        }
        catch (ObjectDisposedException) { /* Ignore */ }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _playbackWakeSignal.Dispose();
        GC.SuppressFinalize(this);
    }
}