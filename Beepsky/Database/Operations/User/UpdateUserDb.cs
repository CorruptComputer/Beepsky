using Beepsky.Database.DbSets;

namespace Beepsky.Database.Operations.User;

/// <inheritdoc />
public sealed class UpdateUserDb(BeepskyDbContext dbContext) : IRequestHandler<UpdateUserDb.Command, CommandResponse>
{
    /// <summary>
    ///   Updates a user in the database
    /// </summary>
    /// <param name="User"></param>
    public sealed record Command(DiscordUser User) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {

        dbContext.DiscordUsers.Update(command.User);
        dbContext.SaveChanges();

        return CommandResponse.Pass();
    }
}