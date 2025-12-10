using Beepsky.Database.DbSets;

namespace Beepsky.Database.Operations.User;

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
        await dbContext.DiscordUsers.AddAsync(command.User, cancellationToken);
        dbContext.SaveChanges();

        return CommandResponse.Pass();
    }
}