using Beepsky.Core.Database.DbSets;

namespace Beepsky.Core.Database.Operations.AudioDownloads;

/// <inheritdoc />
public sealed class UpdateAudioDownload(BeepskyDbContext dbContext) : IRequestHandler<UpdateAudioDownload.Command, CommandResponse>
{
    /// <summary>
    ///   Updates an audio download entry in the database
    /// </summary>
    /// <param name="AudioDownload"></param>
    public sealed record Command(AudioDownload AudioDownload) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {
        dbContext.AudioDownloads.Update(command.AudioDownload);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CommandResponse.Pass();
    }
}
