using Beepsky.Core.Database.DbSets;

namespace Beepsky.Core.Database.Operations.GuildUserStatistic;

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
        dbContext.DiscordGuildUserStatistics.Add(command.GuildUserStatistic);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CommandResponse.Pass();
    }
}