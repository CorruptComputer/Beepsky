using Beepsky.Services;
using NetCord.Gateway;
using NetCord.Services.Commands;
using Serilog;

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
    [Command("yt")]
    public async Task<string> YouTubeAsync(string track)
    {
        Log.Information("YouTubeAsync command started");

        if (Context.Guild is null)
        {
            Log.Warning("PlaySound attempted outside of guild");
            return "This command can only be used in a guild!";
        }

        Log.Information("Guild resolved: {GuildId}", Context.Guild.Id);

        // Get the user voice state
        if (!Context.Guild.VoiceStates.TryGetValue(Context.User.Id, out VoiceState? voiceState))
        {
            Log.Warning("User {UserId} not in voice channel", Context.User.Id);
            return "You must be in a voice channel to use this command.";
        }

        ulong voiceChannelId = voiceState.ChannelId.GetValueOrDefault();
        Log.Information("User {UserId} in voice channel {ChannelId}", Context.User.Id, voiceChannelId);

        bool added = audioQueue.AddTrackToDownloadQueue(voiceChannelId, Context.Guild.Id, track);

        return added
            ? "🫡"
            : "Failed to add track to queue. Ensure the link is a valid YouTube URL link.";
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

