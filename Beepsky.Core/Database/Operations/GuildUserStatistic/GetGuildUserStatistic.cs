using Beepsky.Core.Database.DbSets;
using Microsoft.EntityFrameworkCore;

namespace Beepsky.Core.Database.Operations.GuildUserStatistic;

/// <inheritdoc />
public sealed class GetGuildUserStatistic(BeepskyDbContext dbContext) : IRequestHandler<GetGuildUserStatistic.Command, QueryResponse<DiscordGuildUserStatistic>>
{
    /// <summary>
    ///   Gets a guild from the database
    /// </summary>
    /// <param name="GuildId"></param>
    /// <param name="UserId"></param>
    /// <param name="Year"></param>
    public sealed record Command(ulong GuildId, ulong UserId, ushort Year) : IRequest<QueryResponse<DiscordGuildUserStatistic>>;

    /// <inheritdoc />
    public async Task<QueryResponse<DiscordGuildUserStatistic>> Handle(Command command, CancellationToken cancellationToken)
    {
        DiscordGuildUserStatistic? guildUserStatistic = await dbContext.DiscordGuildUserStatistics.FirstOrDefaultAsync(s => s.GuildId == command.GuildId && s.UserId == command.UserId && s.Year == command.Year, cancellationToken);

        if (guildUserStatistic is null)
        {
            return QueryResponse<DiscordGuildUserStatistic>.Fail("GuildUserStatistic not found");
        }

        return guildUserStatistic;
    }
}
