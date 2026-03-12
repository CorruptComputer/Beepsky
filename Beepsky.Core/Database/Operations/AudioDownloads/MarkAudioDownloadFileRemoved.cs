using Beepsky.Core.Database.DbSets;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Beepsky.Core.Database.Operations.AudioDownloads;

/// <inheritdoc />
public sealed class MarkAudioDownloadFileRemoved(BeepskyDbContext dbContext) : IRequestHandler<MarkAudioDownloadFileRemoved.Command, CommandResponse>
{
    /// <summary>
    ///   Marks the file for an audio download as removed so it will be re-downloaded on next play
    /// </summary>
    /// <param name="TrackUrl"></param>
    public sealed record Command(string TrackUrl) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {
        AudioDownload? dbEntry = await dbContext.AudioDownloads
            .FirstOrDefaultAsync(d => d.DownloadUrl == command.TrackUrl, cancellationToken);

        if (dbEntry is null)
        {
            Log.Warning("Could not find DB entry to mark file removed for {TrackUrl}", command.TrackUrl);
            return CommandResponse.Pass();
        }

        dbEntry.FileRemoved = true;
        await dbContext.SaveChangesAsync(cancellationToken);

        return CommandResponse.Pass();
    }
}
