using Beepsky.Core.Services;

namespace Beepsky.Core.Features.Audio;

/// <inheritdoc />
public class SkipCurrentlyPlayingTrackInGuild(AudioQueueService audioQueueService)
    : IRequestHandler<SkipCurrentlyPlayingTrackInGuild.Command, CommandResponse>
{
    /// <summary>
    ///   Command to skip the currently playing track in a guild
    /// </summary>
    /// <param name="GuildId"></param>
    public record Command(ulong GuildId) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
    {
        return audioQueueService.AddSkipForGuild(request.GuildId);
    }
}
