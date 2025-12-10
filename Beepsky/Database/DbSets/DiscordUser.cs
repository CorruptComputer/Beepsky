using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Beepsky.Database.DbSets;

/// <summary>
///   Model for the DiscordUsers table
/// </summary>
[Table("DiscordUsers")]
[PrimaryKey(nameof(UserId))]
public class DiscordUser
{
    /// <summary>
    ///   Discord User ID
    /// </summary>
    public required ulong UserId { get; init; }

    /// <summary>
    ///   Discord User Username
    /// </summary>
    public required string Username { get; set; }

    /// <summary>
    ///   Whether the user can use commands, defaults to true
    /// </summary>
    public bool CanUseCommands { get; set; } = true;

    /// <summary>
    ///   Discord User Mention string
    /// </summary>
    public required string Mention { get; set; }

    internal static void BuildTable(EntityTypeBuilder<DiscordUser> builder)
    {

    }
}