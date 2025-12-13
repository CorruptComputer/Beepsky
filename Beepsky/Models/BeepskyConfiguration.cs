namespace Beepsky.Models;

/// <summary>
///   Configuration for Beepsky.<br />
///   Stored in /etc/beepsky/config.json
/// </summary>
public sealed record BeepskyConfiguration
{
    internal const char Prefix = ';';

    /// <summary>
    ///   The Discord bot token
    /// </summary>
    public required string DiscordBotToken { get; init; }

    /// <summary>
    ///   The database connection string
    /// </summary>
    public required string DatabaseConnectionString { get; init; }

    /// <summary>
    ///   The download cache directory
    /// </summary>
    public required string DownloadCache { get; init; }
}