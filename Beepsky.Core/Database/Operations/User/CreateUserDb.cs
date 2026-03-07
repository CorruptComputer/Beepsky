using Beepsky.Core.Database.DbSets;

namespace Beepsky.Core.Database.Operations.User;

/// <inheritdoc />
public sealed class CreateUserDb(BeepskyDbContext dbContext) : IRequestHandler<CreateUserDb.Command, CommandResponse>
{
    /// <summary>
    ///   Updates a user in the database
    /// </summary>
    /// <param name="User"></param>
    public sealed record Command(DiscordUser User) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {
        dbContext.DiscordUsers.Add(command.User);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CommandResponse.Pass();
    }
}