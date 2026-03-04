using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Beepsky.Database.DbSets;

/// <summary>
///   Model for the AudioDownloads table
/// </summary>
[Table("AudioDownloads")]
[PrimaryKey(nameof(DownloadId))]
public class AudioDownload
{
    /// <summary>
    ///   The ID of the download, omit if new as it will be generated.
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid DownloadId { get; init; }

    /// <summary>
    ///   The URL this download is from
    /// </summary>
    public required string DownloadUrl { get; set; }

    /// <summary>
    ///   The path on the filesystem where the file is saved
    /// </summary>
    public required string FilePath { get; set; }

    /// <summary>
    ///   The title of the downloaded audio track
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    ///   The time this download was created
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    ///   The last time this download was accessed
    /// </summary>
    public required DateTimeOffset LastAccessedAt { get; set; }

    /// <summary>
    ///   The number of times this download has been played
    /// </summary>
    public required int PlayCount { get; set; }

    /// <summary>
    ///   True if the file was removed from disk after download; causes the download service to re-download
    /// </summary>
    public bool FileRemoved { get; set; }

    internal static void BuildTable(EntityTypeBuilder<AudioDownload> builder)
    {

    }
}