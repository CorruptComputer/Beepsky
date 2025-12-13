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
    [Command("q")]
    public Task<string> QueueTrack(string track)
    {
        Log.Information("QueueTrack command started");

        if (Context.Guild is null)
        {
            Log.Warning("QueueTrack attempted outside of guild");
            return Task.FromResult("This command can only be used in a guild.");
        }

        Log.Information("Guild resolved: {GuildId}", Context.Guild.Id);

        // Get the user voice state
        if (!Context.Guild.VoiceStates.TryGetValue(Context.User.Id, out VoiceState? voiceState))
        {
            Log.Warning("User {UserId} not in voice channel", Context.User.Id);
            return Task.FromResult("You must be in a voice channel to use this command.");
        }

        ulong voiceChannelId = voiceState.ChannelId.GetValueOrDefault();
        Log.Information("User {UserId} in voice channel {ChannelId}", Context.User.Id, voiceChannelId);

        bool added = audioQueue.AddTrackToQueue(voiceChannelId, Context.Guild.Id, track);

        return Task.FromResult(added
            ? "🫡"
            : "Failed to add track to queue. Ensure the link is valid.");
    }

    /// <summary>
    ///   Skip the current track
    /// </summary>
    /// <returns></returns>
    [Command("skip")]
    public string SkipTrack()
    {
        Log.Information("SkipTrack command started");

        if (Context.Guild is null)
        {
            Log.Warning("SkipTrack attempted outside of guild");
            return "This command can only be used in a guild.";
        }

        Log.Information("Guild resolved: {GuildId}", Context.Guild.Id);

        // Get the user voice state
        if (!Context.Guild.VoiceStates.TryGetValue(Context.User.Id, out VoiceState? voiceState))
        {
            Log.Warning("User {UserId} not in voice channel", Context.User.Id);
            return "You must be in a voice channel to use this command.";
        }

        audioQueue.AddSkipForGuild(Context.Guild.Id);
        Log.Information("Skip requested for guild {GuildId} by user {UserId}", Context.Guild.Id, Context.User.Id);
        return "🫡";
    }

    /// <summary>
    ///   Stop all playback in the guild
    /// </summary>
    /// <returns></returns>
    [Command("stop")]
    public string StopAllPlayback()
    {
        Log.Information("StopTrack command started");

        if (Context.Guild is null)
        {
            Log.Warning("StopTrack attempted outside of guild");
            return "This command can only be used in a guild.";
        }

        Log.Information("Guild resolved: {GuildId}", Context.Guild.Id);

        // Get the user voice state
        if (!Context.Guild.VoiceStates.TryGetValue(Context.User.Id, out VoiceState? voiceState))
        {
            Log.Warning("User {UserId} not in voice channel", Context.User.Id);
            return "You must be in a voice channel to use this command.";
        }

        audioQueue.AddStopForGuild(Context.Guild.Id);
        Log.Information("Stop requested for guild {GuildId} by user {UserId}", Context.Guild.Id, Context.User.Id);

        return "🫡";
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

