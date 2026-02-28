using Beepsky.Database.DbSets;
using Microsoft.EntityFrameworkCore;

namespace Beepsky.Database.Operations.AudioDownloads;

/// <inheritdoc />
public sealed class GetAudioDownloadByUrl(BeepskyDbContext dbContext) : IRequestHandler<GetAudioDownloadByUrl.Command, QueryResponse<AudioDownload>>
{
    /// <summary>
    ///   Gets an audio download entry by its download URL
    /// </summary>
    /// <param name="DownloadUrl"></param>
    public sealed record Command(string DownloadUrl) : IRequest<QueryResponse<AudioDownload>>;

    /// <inheritdoc />
    public async Task<QueryResponse<AudioDownload>> Handle(Command command, CancellationToken cancellationToken)
    {
        AudioDownload? download = await dbContext.AudioDownloads
            .FirstOrDefaultAsync(d => d.DownloadUrl == command.DownloadUrl, cancellationToken);

        if (download is null)
        {
            return QueryResponse<AudioDownload>.Fail("AudioDownload not found");
        }

        return download;
    }
}
