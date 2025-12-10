using Beepsky.Database.DbSets;

namespace Beepsky.Database.Operations.GuildUserStatistic;

/// <inheritdoc />
public sealed class CreateGuildUserStatistic(BeepskyDbContext dbContext) : IRequestHandler<CreateGuildUserStatistic.Command, CommandResponse>
{
    /// <summary>
    ///   Updates a guild in the database
    /// </summary>
    /// <param name="GuildUserStatistic"></param>
    public sealed record Command(DiscordGuildUserStatistic GuildUserStatistic) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {
        await dbContext.DiscordGuildUserStatistics.AddAsync(command.GuildUserStatistic, cancellationToken);
        dbContext.SaveChanges();

        return CommandResponse.Pass();
    }
}