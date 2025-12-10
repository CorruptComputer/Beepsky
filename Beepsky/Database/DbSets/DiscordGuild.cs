using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Beepsky.Database.DbSets;

/// <summary>
///   Model for the DiscordGuilds table
/// </summary>
[Table("DiscordGuilds")]
[PrimaryKey(nameof(GuildId))]
public class DiscordGuild
{
    /// <summary>
    ///   Discord Guild ID
    /// </summary>
    public required ulong GuildId { get; init; }

    /// <summary>
    ///   Discord Guild Name
    /// </summary>
    public required string Name { get; set; }

    internal static void BuildTable(EntityTypeBuilder<DiscordGuild> builder)
    {

    }
}
