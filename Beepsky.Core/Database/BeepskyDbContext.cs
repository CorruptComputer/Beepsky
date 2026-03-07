using Beepsky.Core.Exceptions;
using Beepsky.Core.Database.DbSets;
using Microsoft.EntityFrameworkCore;

namespace Beepsky.Core.Database;

/// <summary>
///   Beepsky database context
/// </summary>
/// <param name="config"></param>
public sealed class BeepskyDbContext(BeepskyConfiguration config) : DbContext
{
    internal DbSet<AudioDownload> AudioDownloads { get; set; }
    internal DbSet<DiscordGuild> DiscordGuilds { get; set; }

    internal DbSet<DiscordGuildUserStatistic> DiscordGuildUserStatistics { get; set; }

    internal DbSet<DiscordUser> DiscordUsers { get; set; }

    /// <summary>
    ///   Configure which database to use: PostgreSQL in most cases, in-memory DB for unit tests.
    /// </summary>
    /// <param name="optionsBuilder"></param>
    /// <exception cref="BeepskyException"></exception>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (string.IsNullOrWhiteSpace(config.DatabaseConnectionString))
        {
            throw new BeepskyException("BeepskyConfiguration:DatabaseConnectionString is missing.");
        }

        optionsBuilder.UseNpgsql(config.DatabaseConnectionString,
            options =>
            {
                options.MigrationsHistoryTable("__EFMigrationsHistory");
                options.MigrationsAssembly(typeof(BeepskyDbContext).Assembly.FullName);
                options.EnableRetryOnFailure();
            }
        );
    }

    /// <summary>
    ///   Create models
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AudioDownload>(AudioDownload.BuildTable);
        modelBuilder.Entity<DiscordGuild>(DiscordGuild.BuildTable);
        modelBuilder.Entity<DiscordGuildUserStatistic>(DiscordGuildUserStatistic.BuildTable);
        modelBuilder.Entity<DiscordUser>(DiscordUser.BuildTable);

        base.OnModelCreating(modelBuilder);
    }
}
