using System.Text;
using Beepsky.Core.Database.DbSets;
using Beepsky.Core.Database.Operations.AudioDownloads;
using Beepsky.Core.Features.Audio;
using Beepsky.Core.Features.Audio.QuickQueue;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services.Commands;

namespace Beepsky.DiscordEventHandlers.Commands.Text;

/// <inheritdoc />
public class AudioCommandModule(ISender sender) : CommandModule<CommandContext>
{
    /// <summary>
    ///   YouTube command
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [Command("q")]
    public async Task<string> QueueTrackAsync([CommandParameter(Remainder = true)] string track)
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
            added = await sender.Send(new QueueTrackInGuild.Command(Context.Guild.Id, voiceChannelId, track));
        }

        return added
            ? "🫡"
            : "Failed to add track to queue. Ensure the link is valid.";
    }

    private async Task<bool> TryQuickQueueTracksAsync(string track, ulong guildId, ulong voiceChannelId)
    {
        IRequest<CommandResponse>? quickSelect = track.ToLowerInvariant() switch
        {
            "anuc" => new QueueRandomAnucSongsInGuild.Command(guildId, voiceChannelId),
            "christmas" => new QueueRandomChristmasSongsInGuild.Command(guildId, voiceChannelId),
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
    public async Task<string> SkipTrackAsync()
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

        await sender.Send(new SkipCurrentlyPlayingTrackInGuild.Command(Context.Guild.Id));

        return "🫡";
    }

    /// <summary>
    ///   Stop all playback in the guild
    /// </summary>
    /// <returns></returns>
    [Command("stop")]
    public async Task<string> StopAllPlaybackAsync()
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

        await sender.Send(new StopAllTracksForGuild.Command(Context.Guild.Id));

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

        string? response = await sender.Send(new GetTrackQueueForGuild.Query(Context.Guild.Id));
        if (response is null)
        {
            await Context.Message.ReplyAsync("Failed to retrieve queue information.");
            return;
        }

        await Context.Message.ReplyAsync(new ReplyMessageProperties()
        {
            Content = response,
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

