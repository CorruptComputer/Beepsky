using Beepsky.Database.DbSets;

namespace Beepsky.Database.Operations.AudioDownloads;

/// <inheritdoc />
public sealed class CreateAudioDownload(BeepskyDbContext dbContext) : IRequestHandler<CreateAudioDownload.Command, CommandResponse>
{
    /// <summary>
    ///   Creates an audio download entry in the database
    /// </summary>
    /// <param name="AudioDownload"></param>
    public sealed record Command(AudioDownload AudioDownload) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {
        dbContext.AudioDownloads.Add(command.AudioDownload);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CommandResponse.Pass();
    }
}
