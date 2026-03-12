using Beepsky.Core.Database.DbSets;
using Serilog;

namespace Beepsky.Core.Database.Operations.AudioDownloads;

/// <inheritdoc />
public sealed class UpsertAudioDownload(BeepskyDbContext dbContext) : IRequestHandler<UpsertAudioDownload.Command, CommandResponse>
{
    /// <summary>
    ///   Creates or updates an audio download entry in the database.
    ///   If <paramref name="ExistingEntry"/> is provided, that entry is updated in-place.
    ///   Otherwise a new entry is created using <paramref name="DownloadUrl"/> and <paramref name="FilePath"/>.
    /// </summary>
    /// <param name="DownloadUrl">The source URL of the download</param>
    /// <param name="FilePath">The local path where the file is stored</param>
    /// <param name="Title">The track title; only applied if non-empty</param>
    /// <param name="ExistingEntry">An existing DB entry to update, or null to create a new one</param>
    public sealed record Command(string DownloadUrl, string FilePath, string? Title, AudioDownload? ExistingEntry) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.ExistingEntry is not null)
            {
                command.ExistingEntry.FileRemoved = false;
                command.ExistingEntry.FilePath = command.FilePath;
                if (!string.IsNullOrEmpty(command.Title))
                {
                    command.ExistingEntry.Title = command.Title;
                }
                command.ExistingEntry.LastAccessedAt = DateTimeOffset.UtcNow;
                dbContext.AudioDownloads.Update(command.ExistingEntry);
                Log.Information("Updated audio download metadata in DB for {Url}", command.DownloadUrl);
            }
            else
            {
                AudioDownload newEntry = new()
                {
                    DownloadUrl = command.DownloadUrl,
                    FilePath = command.FilePath,
                    Title = command.Title ?? string.Empty,
                    CreatedAt = DateTimeOffset.UtcNow,
                    LastAccessedAt = DateTimeOffset.UtcNow,
                    PlayCount = 0
                };
                dbContext.AudioDownloads.Add(newEntry);
                Log.Information("Saved audio download metadata to DB for {Url}", command.DownloadUrl);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return CommandResponse.Pass();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving audio download metadata to DB for {Url}", command.DownloadUrl);
            return CommandResponse.Fail();
        }
    }
}
