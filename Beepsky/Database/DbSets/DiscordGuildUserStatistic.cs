using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Beepsky.Database.DbSets;

/// <summary>
///   Model for the DiscordGuildUserStatistics table
/// </summary>
[Table("DiscordGuildUserStatistics")]
[PrimaryKey(nameof(DiscordGuildUserStatisticId))]
public class DiscordGuildUserStatistic
{
    /// <summary>
    ///   Unique ID for the guild user statistic entry
    /// </summary>
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid DiscordGuildUserStatisticId { get; set; }

    /// <summary>
    ///   Discord Guild ID
    /// </summary>
    public required ulong GuildId { get; init; }

    /// <summary>
    ///   Discord User ID
    /// </summary>
    public required ulong UserId { get; init; }

    /// <summary>
    ///   Year of the statistics
    /// </summary>
    public required ushort Year { get; init; }

    /// <summary>
    ///   Total messages sent by the user in the guild for the year
    /// </summary>
    public required long MessagesSent { get; set; }

    /// <summary>
    ///   Total Beepsky commands used by the user in the guild for the year
    /// </summary>
    public required long BeepskyCommandsUsed { get; set; }

    /// <summary>
    ///   Total times Beepsky chatted with the user in the guild for the year
    /// </summary>
    public required long BeepskyChattedWith { get; set; }

    /// <summary>
    ///   Most chatty day for the user in the guild
    /// </summary>
    public required DateOnly MostChattyDay { get; set; }

    /// <summary>
    ///   Messages sent on the most chatty day
    /// </summary>
    public required long MessagesSentOnMostChattyDay { get; set; }

    /// <summary>
    ///   Date representing today for daily statistics
    /// </summary>
    public required DateOnly Today { get; set; }

    /// <summary>
    ///   Messages sent today
    /// </summary>
    public required long MessagesSentToday { get; set; }

    /// <summary>
    ///   Times joined voice channels
    /// </summary>
    public required long TimesJoinedVoice { get; set; }

    /// <summary>
    ///   Times kicked from voice channels
    /// </summary>
    public required long TimesKickedFromVoice { get; set; }

    /// <summary>
    ///   Messages deleted from this user
    /// </summary>
    public required long MessagesDeletedFrom { get; set; }

    /// <summary>
    ///   Messages deleted by this user
    /// </summary>
    public required long MessagesDeletedBy { get; set; }

    internal static void BuildTable(EntityTypeBuilder<DiscordGuildUserStatistic> builder)
    {
        builder.HasIndex(p => p.GuildId);
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.Year);

        builder.HasOne<DiscordGuild>()
            .WithMany()
            .HasForeignKey(nameof(GuildId));

        builder.HasOne<DiscordUser>()
            .WithMany()
            .HasForeignKey(nameof(UserId));
    }
}
