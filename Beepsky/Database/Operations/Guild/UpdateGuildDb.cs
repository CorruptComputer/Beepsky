using Beepsky.Database.DbSets;

namespace Beepsky.Database.Operations.Guild;

/// <inheritdoc />
public sealed class UpdateGuildDb(BeepskyDbContext dbContext) : IRequestHandler<UpdateGuildDb.Command, CommandResponse>
{
    /// <summary>
    ///   Updates a guild in the database
    /// </summary>
    /// <param name="Guild"></param>
    public sealed record Command(DiscordGuild Guild) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {
        dbContext.DiscordGuilds.Update(command.Guild);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CommandResponse.Pass();
    }
}