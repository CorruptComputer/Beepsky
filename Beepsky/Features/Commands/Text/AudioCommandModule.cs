using System.Text;
using Beepsky.Database.DbSets;
using Beepsky.Database.Operations.AudioDownloads;
using Beepsky.Features.Audio;
using Beepsky.Services;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace Beepsky.Features.Commands.Text;

/// <inheritdoc />
public class AudioCommandModule(AudioQueueService audioQueue, ISender sender) : CommandModule<CommandContext>
{
    /// <summary>
    ///   YouTube command
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [Command("q")]
    public async Task<string> QueueTrackAsync(string track)
    {

        if (Context.Guild is null)
        {
            return "This command can only be used in a guild.";
        }

        // Get the user voice state
        if (!Context.Guild.VoiceStates.TryGetValue(Context.User.Id, out VoiceState? voiceState))
        {
            return "You must be in a voice channel to use this command.";
        }

        ulong voiceChannelId = voiceState.ChannelId.GetValueOrDefault();

        // Quick queues are just named playlists, it picks a random set of songs if the name matches
        bool added = await TryQuickQueueTracksAsync(track, Context.Guild.Id, voiceChannelId);
        if (!added)
        {
            added = await audioQueue.AddTrackToQueue(voiceChannelId, Context.Guild.Id, track);
        }

        return added
            ? "🫡"
            : "Failed to add track to queue. Ensure the link is valid.";
    }

    private async Task<bool> TryQuickQueueTracksAsync(string track, ulong guildId, ulong voiceChannelId)
    {
        IRequest<CommandResponse>? quickSelect = track.ToLowerInvariant() switch
        {
            "anuc" => new QueueRandomAnucSongs.Command(guildId, voiceChannelId),
            "christmas" => new QueueRandomChristmasSongs.Command(guildId, voiceChannelId),
            _ => null,
        };

        if (quickSelect is null)
        {
            return false;
        }

        await sender.Send(quickSelect);

        return true;
    }

    /// <summary>
    ///   Skip the current track
    /// </summary>
    /// <returns></returns>
    [Command("skip")]
    public string SkipTrack()
    {
        if (Context.Guild is null)
        {
            return "This command can only be used in a guild.";
        }

        // Get the user voice state
        if (!Context.Guild.VoiceStates.TryGetValue(Context.User.Id, out VoiceState? voiceState))
        {
            return "You must be in a voice channel to use this command.";
        }

        audioQueue.AddSkipForGuild(Context.Guild.Id);
        return "🫡";
    }

    /// <summary>
    ///   Stop all playback in the guild
    /// </summary>
    /// <returns></returns>
    [Command("stop")]
    public string StopAllPlayback()
    {

        if (Context.Guild is null)
        {
            return "This command can only be used in a guild.";
        }

        // Get the user voice state
        if (!Context.Guild.VoiceStates.TryGetValue(Context.User.Id, out VoiceState? voiceState))
        {
            return "You must be in a voice channel to use this command.";
        }

        audioQueue.AddStopForGuild(Context.Guild.Id);

        return "🫡";
    }

    /// <summary>
    ///   Lists the current queue for the guild
    /// </summary>
    /// <returns></returns>
    [Command("lq")]
    public async Task ListQueueAsync()
    {
        if (Context.Guild is null)
        {
            await Context.Message.ReplyAsync("This command can only be used in a guild.");
            return;
        }

        QueuedAudioTrack? currentlyPlaying = audioQueue.GetCurrentlyPlayingTrackForGuild(Context.Guild.Id);
        IOrderedEnumerable<QueuedAudioTrack> queue = audioQueue.GetQueueForGuild(Context.Guild.Id).OrderBy(track => track.QueuedAt);

        if (currentlyPlaying is null && !queue.Any())
        {
            await Context.Message.ReplyAsync("The queue is currently empty.");
            return;
        }

        StringBuilder response = new($"Currently Playing: ");
        if (currentlyPlaying is not null)
        {
            response.Append(GetFormattedTrackTitleFromQueuedAudioTrack(currentlyPlaying));
        }
        else
        {
            response.Append("Nothing");
        }

        bool hasSkip = audioQueue.GetGuildsWithSkips().Contains(Context.Guild.Id);
        if (hasSkip)
        {
            response.Append("\n*A skip has been requested for the currently playing track.*");
        }

        if (queue.Any())
        {
            response.Append("\n\nUp Next:\n");
            int index = 1;
            foreach (QueuedAudioTrack track in queue)
            {
                response.Append(index);
                response.Append(". ");
                response.Append(GetFormattedTrackTitleFromQueuedAudioTrack(track));
                response.Append(" (");
                response.Append(Enum.GetName(track.CurrentState));
                response.Append(")\n");
                index++;
            }
        }

        await Context.Message.ReplyAsync(new ReplyMessageProperties()
        {
            Content = response.ToString(),
            Flags = MessageFlags.SuppressEmbeds
        });
    }

    /// <summary>
    ///   Shows the top 10 most played tracks
    /// </summary>
    /// <returns></returns>
    [Command("top")]
    public async Task TopTracksAsync()
    {
        // Not ready yet, need more data in the db since this count tracking was added recently
        if (Context.User.Id != (ulong)WellKnownUsers.Monke)
        {
            return;
        }

        List<AudioDownload>? topTracks = await sender.Send(new GetTopAudioTracks.Command());
        if (topTracks is null || topTracks.Count == 0)
        {
            await Context.Message.ReplyAsync("No tracks have been played yet.");
            return;
        }

        StringBuilder response = new("**Top tracks for Beepsky:**\n");
        int index = 1;
        foreach (AudioDownload track in topTracks)
        {
            response.Append(index);
            response.Append(". ");
            response.Append(GetFormattedTrackTitleFromAudioDownload(track));
            response.Append(" (");
            response.Append(track.PlayCount);
            response.Append(" plays)\n");
            index++;
        }

        await Context.Message.ReplyAsync(new ReplyMessageProperties()
        {
            Content = response.ToString(),
            Flags = MessageFlags.SuppressEmbeds
        });
    }

    private static string GetFormattedTrackTitleFromQueuedAudioTrack(QueuedAudioTrack track)
    {
        string title = string.Empty;

        if (track.Title is not null)
        {
            title += $"[{track.Title}]({track.TrackUri})";
        }
        else
        {
            title += track.TrackUri.ToString();
        }

        return title;
    }

    private static string GetFormattedTrackTitleFromAudioDownload(AudioDownload track)
    {
        string title = string.Empty;

        if (track.Title is not null)
        {
            title += $"[{track.Title}]({track.DownloadUrl})";
        }
        else
        {
            title += track.DownloadUrl.ToString();
        }

        return title;
    }

    // These add some nice flavor, but felt a little too much to me
    //private static readonly string[] positiveResponses =
    //[
    //    "🫡 REQUEST RECEIVED. QUEUEING UNDER PROTOCOL Q-17.",
    //    "🫡 ORDER LOGGED. PRIORITIZATION SUBROUTINES ENGAGED.",
    //    "🫡 DIRECTIVE ACCEPTED. TASK ENTERED INTO THE ENFORCEMENT QUEUE.",
    //    "🫡 INPUT VERIFIED. PROCESSING PIPELINE UPDATED.",
    //    "🫡 COMMAND STORED. EXECUTION QUEUE STATUS: ARMED AND WAITING.",
    //    "🫡 DATA RECEIPT CONFIRMED. ITEM MOVED TO ACTIVE QUEUE.",
    //    "🫡 ACKNOWLEDGED. YOUR REQUEST HAS BEEN FILED FOR LAWFUL PROCESSING.",
    //    "🫡 SUBMISSION ACCEPTED. TASK SCHEDULED FOR PROCESSING CYCLE.",
    //];
}

