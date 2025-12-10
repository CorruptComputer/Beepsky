using Beepsky.Database.DbSets;

namespace Beepsky.Database.Operations.Guild;

/// <inheritdoc />
public sealed class GetGuildDb(BeepskyDbContext dbContext) : IRequestHandler<GetGuildDb.Command, QueryResponse<DiscordGuild>>
{
    /// <summary>
    ///   Gets a guild from the database
    /// </summary>
    /// <param name="GuildId"></param>
    public sealed record Command(ulong GuildId) : IRequest<QueryResponse<DiscordGuild>>;

    /// <inheritdoc />
    public async Task<QueryResponse<DiscordGuild>> Handle(Command command, CancellationToken cancellationToken)
    {
        DiscordGuild? guild = await dbContext.DiscordGuilds.FindAsync([command.GuildId], cancellationToken);

        if (guild is null)
        {
            return QueryResponse<DiscordGuild>.Fail("Guild not found");
        }

        return guild;
    }
}
