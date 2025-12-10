using Beepsky.Database.DbSets;

namespace Beepsky.Database.Operations.Guild;

/// <inheritdoc />
public sealed class CreateGuildDb(BeepskyDbContext dbContext) : IRequestHandler<CreateGuildDb.Command, CommandResponse>
{
    /// <summary>
    ///   Updates a guild in the database
    /// </summary>
    /// <param name="Guild"></param>
    public sealed record Command(DiscordGuild Guild) : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {
        await dbContext.DiscordGuilds.AddAsync(command.Guild, cancellationToken);
        dbContext.SaveChanges();

        return CommandResponse.Pass();
    }
}