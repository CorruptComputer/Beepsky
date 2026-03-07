using Beepsky.Services;

namespace Beepsky.Features.Audio;

/// <inheritdoc />
public class StopAllTracksForGuild(AudioQueueService audioQueueService)
    : IRequestHandler<StopAllTracksForGuild.Command, CommandResponse>
{
    /// <summary>
    ///   Command to stop all tracks in a guild
    /// </summary>
    /// <param name="GuildId"></param>
    public record Command(ulong GuildId) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command request, CancellationToken cancellationToken)
    {
        return audioQueueService.AddStopForGuild(request.GuildId);
    }
}
