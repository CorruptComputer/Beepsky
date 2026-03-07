using Beepsky.Core.Database.DbSets;

namespace Beepsky.Core.Database.Operations.User;

/// <inheritdoc />
public sealed class GetUserDb(BeepskyDbContext dbContext) : IRequestHandler<GetUserDb.Command, QueryResponse<DiscordUser>>
{
    /// <summary>
    ///   Gets a user from the database
    /// </summary>
    /// <param name="UserId"></param>
    public sealed record Command(ulong UserId) : IRequest<QueryResponse<DiscordUser>>;

    /// <inheritdoc />
    public async Task<QueryResponse<DiscordUser>> Handle(Command command, CancellationToken cancellationToken)
    {
        DiscordUser? user = await dbContext.DiscordUsers.FindAsync([command.UserId], cancellationToken);

        if (user is null)
        {
            return QueryResponse<DiscordUser>.Fail("User not found");
        }

        return user;
    }
}