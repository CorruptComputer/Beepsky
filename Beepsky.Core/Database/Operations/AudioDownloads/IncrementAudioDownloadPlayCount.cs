using Beepsky.Core.Database.DbSets;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Beepsky.Core.Database.Operations.AudioDownloads;

/// <inheritdoc />
public sealed class IncrementAudioDownloadPlayCount(BeepskyDbContext dbContext) : IRequestHandler<IncrementAudioDownloadPlayCount.Command, CommandResponse>
{
    /// <summary>
    ///   Increments the play count and updates the last accessed timestamp for an audio download
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
            Log.Warning("Could not find DB entry to increment play count for {TrackUrl}", command.TrackUrl);
            return CommandResponse.Pass();
        }

        dbEntry.PlayCount++;
        dbEntry.LastAccessedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return CommandResponse.Pass();
    }
}
