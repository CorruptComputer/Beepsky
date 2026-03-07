using Beepsky.Core.Database.DbSets;
using Microsoft.EntityFrameworkCore;

namespace Beepsky.Core.Database.Operations.AudioDownloads;

/// <inheritdoc />
public sealed class GetTopAudioTracks(BeepskyDbContext dbContext) : IRequestHandler<GetTopAudioTracks.Command, QueryResponse<List<AudioDownload>>>
{
    /// <summary>
    ///   Gets the top 10 most played audio tracks
    /// </summary>
    public sealed record Command() : IRequest<QueryResponse<List<AudioDownload>>>;

    /// <inheritdoc />
    public async Task<QueryResponse<List<AudioDownload>>> Handle(Command command, CancellationToken cancellationToken)
    {
        List<AudioDownload> topTracks = await dbContext.AudioDownloads
            .OrderByDescending(d => d.PlayCount)
            .Take(10)
            .ToListAsync(cancellationToken);

        return topTracks;
    }
}
