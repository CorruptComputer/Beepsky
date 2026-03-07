using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Beepsky.Core.Database.Operations;

/// <inheritdoc />
public sealed class MigrateDb(BeepskyDbContext dbContext) : IRequestHandler<MigrateDb.Command, CommandResponse>
{
    /// <summary>
    ///   Migrates the database to the latest version
    /// </summary>
    public sealed record Command() : IRequest<CommandResponse>;

    /// <inheritdoc />
    public async Task<CommandResponse> Handle(Command command, CancellationToken cancellationToken)
    {
        IEnumerable<string> pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pendingMigrations.Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            Log.Information("Migration complete.");
            return CommandResponse.Pass();
        }

        Log.Information("No pending migrations.");
        return CommandResponse.Pass();
    }
}