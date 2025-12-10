using Beepsky.Database.DbSets;

namespace Beepsky.Database.Operations.GuildUserStatistic;

/// <inheritdoc />
public sealed class UpdateGuildUserStatistic(BeepskyDbContext dbContext) : IRequestHandler<UpdateGuildUserStatistic.Command, CommandResponse>
{
    /// <summary>
    ///   Updates a guild in the database
    /// </summary>
    /// <param name="GuildUserStatistic"></param>
    public sealed record Command(DiscordGuildUserStatistic GuildUserStatistic) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {
        dbContext.DiscordGuildUserStatistics.Update(command.GuildUserStatistic);
        dbContext.SaveChanges();

        return CommandResponse.Pass();
    }
}