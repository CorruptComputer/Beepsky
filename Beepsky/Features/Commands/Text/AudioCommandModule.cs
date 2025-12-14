using System.Text;
using Beepsky.Services;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace Beepsky.Features.Commands.Text;

/// <inheritdoc />
public class AudioCommandModule(AudioQueueService audioQueue) : CommandModule<CommandContext>
{
    /// <summary>
    ///   YouTube command
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [Command("q")]
    public string QueueTrack(string track)
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

        bool added = audioQueue.AddTrackToQueue(voiceChannelId, Context.Guild.Id, track);

        return added
            ? "🫡"
            : "Failed to add track to queue. Ensure the link is valid.";
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
    public async Task ListQueue()
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
            response.Append(GetFormattedTrackTitle(currentlyPlaying));
        }
        else
        {
            response.Append("Nothing");
        }

        if (queue.Any())
        {
            response.Append("\n\nUp Next:\n");
            int index = 1;
            foreach (QueuedAudioTrack track in queue)
            {
                response.Append(index);
                response.Append(". ");
                response.Append(GetFormattedTrackTitle(track));
                response.Append(" (");
                response.Append(Enum.GetName(track.CurrentState));
                response.Append(")\n");
                index++;
            }
        }

        RestMessage reply = await Context.Message.ReplyAsync(new ReplyMessageProperties()
        {
            Content = response.ToString(),
            Flags = MessageFlags.SuppressEmbeds
        });
    }

    private static string GetFormattedTrackTitle(QueuedAudioTrack track)
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

    // These add some nice flavor, but felt a little too much to me
    //private static readonly Random rdm = new();
    //private static readonly List<string> positiveResponses =
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

