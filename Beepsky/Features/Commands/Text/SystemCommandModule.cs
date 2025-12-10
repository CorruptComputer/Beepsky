using System;
using Microsoft.Extensions.Hosting;
using NetCord.Services.Commands;

namespace Beepsky.Features.Commands.Text;

/// <inheritdoc />
public class SystemCommandModule(IHostApplicationLifetime hostApplicationLifetime) : CommandModule<CommandContext>
{
    /// <summary>
    ///   Stops the bot
    /// </summary>
    /// <returns></returns>
    [Command("stop")]
    public string Stop()
    {
        // Ideally all the commands in this should have these same restrictions, need to look into these further:
        // https://netcord.dev/guides/services/preconditions.html
        if (IsChannelDm() && IsUserAdmin())
        {
            hostApplicationLifetime.StopApplication();
        }

        return string.Empty;
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
