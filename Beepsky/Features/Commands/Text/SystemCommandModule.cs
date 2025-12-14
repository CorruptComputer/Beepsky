using Microsoft.Extensions.Hosting;
using NetCord.Services.Commands;

namespace Beepsky.Features.Commands.Text;

/// <inheritdoc />
public class SystemCommandModule(IHostApplicationLifetime hostApplicationLifetime) : CommandModule<CommandContext>
{
    /// <summary>
    ///   Shuts down the bot
    /// </summary>
    /// <returns></returns>
    [Command("shutdown")]
    public async Task StopAsync()
    {
        // Ideally all the commands in this module should have these same restrictions, need to look into these further:
        // https://netcord.dev/guides/services/preconditions.html
        if (IsChannelDm() && IsUserAdmin())
        {
            await Context.Message.ReplyAsync("GOODBYE.");
            hostApplicationLifetime.StopApplication();
        }
    }

    private bool IsUserAdmin()
    {
        return Context.Message.Author.Id == (ulong)WellKnownUsers.Monke;
    }

    private bool IsChannelDm()
    {
        return Context.Message.Guild is null;
    }
}
