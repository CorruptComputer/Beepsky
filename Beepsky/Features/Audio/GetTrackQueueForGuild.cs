using System;
using System.Text;
using Beepsky.Services;

namespace Beepsky.Features.Audio;

/// <inheritdoc />
public class GetTrackQueueForGuild(AudioQueueService audioQueueService)
    : IRequestHandler<GetTrackQueueForGuild.Query, QueryResponse<string>>
{
    /// <summary>
    ///   Command to stop all tracks in a guild
    /// </summary>
    /// <param name="GuildId"></param>
    public record Query(ulong GuildId) : IRequest<QueryResponse<string>>;

    /// <inheritdoc />
    public async Task<QueryResponse<string>> Handle(Query request, CancellationToken cancellationToken)
    {
        QueuedAudioTrack? currentlyPlaying = audioQueueService.GetCurrentlyPlayingTrackForGuild(request.GuildId);
        IOrderedEnumerable<QueuedAudioTrack> queue = audioQueueService.GetQueueForGuild(request.GuildId).OrderBy(track => track.QueuedAt);

        if (currentlyPlaying is null && !queue.Any())
        {
            return "The queue is currently empty.";
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

        bool hasSkip = audioQueueService.GetGuildsWithSkips().Contains(request.GuildId);
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

        return response.ToString();
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
}