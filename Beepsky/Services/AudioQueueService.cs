using System.Collections.Concurrent;
using NetCord.Logging;
using Serilog;

namespace Beepsky.Services;

/// <summary>
///   Handles audio playback functionality
/// </summary>
public class AudioQueueService
{
    private readonly ConcurrentQueue<QueuedAudioTrack> DownloadQueue = [];

    private readonly ConcurrentDictionary<ulong, ConcurrentQueue<QueuedAudioTrack>> ServerPlaybackQueues = [];

    /// <summary>
    ///   Adds a track to the download queue, once downloaded it will be moved to the playback queue
    /// </summary>
    /// <param name="voiceChannelId"></param>
    /// <param name="guildId"></param>
    /// <param name="track"></param>
    /// <returns></returns>
    public bool AddTrackToDownloadQueue(ulong voiceChannelId, ulong guildId, string track)
    {
        bool valid = Uri.TryCreate(track, UriKind.Absolute, out Uri? result);
        Log.Information("URI validation: Valid={Valid}", valid);

        if (!valid
            || result is null
            || !result.IsWellFormedOriginalString()
            || result.Host is not ("www.youtube.com" or "youtube.com" or "youtu.be"))
        {
            Log.Warning("Invalid track URL provided: {Link}", track);
            return false;
        }

        DownloadQueue.Enqueue(new QueuedAudioTrack
        {
            GuildId = guildId,
            VoiceChannelId = voiceChannelId,
            Type = QueuedAudioTrack.DownloadType.YouTube,
            TrackUri = result
        });

        return true;
    }

    /// <summary>
    ///   Adds a track to the playback queue for a server
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    public bool AddTrackToPlaybackQueue(QueuedAudioTrack track)
    {
        if (track.DownloadedFilePath is null)
        {
            return false;
        }

        ConcurrentQueue<QueuedAudioTrack> serverQueue = ServerPlaybackQueues.GetOrAdd(track.GuildId, _ => []);
        serverQueue.Enqueue(track);
        return true;
    }

    /// <summary>
    ///   Gets a list of guilds that have queues
    /// </summary>
    /// <returns></returns>
    public ICollection<ulong> GetGuildsWithPlaybackQueues()
    {
        return ServerPlaybackQueues.Keys;
    }

    /// <summary>
    ///   Pops the next track from the queue for a guild
    /// </summary>
    /// <param name="guildId"></param>
    /// <returns></returns>
    public QueuedAudioTrack? PopNextPlayback(ulong guildId)
    {
        if (ServerPlaybackQueues.TryGetValue(guildId, out ConcurrentQueue<QueuedAudioTrack>? serverPlaybackQueue))
        {
            if (serverPlaybackQueue.TryDequeue(out QueuedAudioTrack? nextTrack))
            {
                return nextTrack;
            }
        }

        return null;
    }

    /// <summary>
    ///   Pops the next track from the queue for a guild
    /// </summary>
    /// <returns></returns>
    public QueuedAudioTrack? PopNextDownload()
    {
        if (DownloadQueue.TryDequeue(out QueuedAudioTrack? trackToDownload))
        {
            return trackToDownload;
        }

        return null;
    }
}