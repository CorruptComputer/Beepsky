using Autofac;
using Autofac.Extensions.DependencyInjection;
using Beepsky.Extensions;
using Beepsky.Core.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using NetCord.Hosting.Services.Commands;
using Beepsky.DiscordEventHandlers.Commands.Text;
using Beepsky.Core.Database.Operations;
using Beepsky.Core.Extensions;
using Beepsky.Core;

namespace Beepsky;

/// <summary>
///   The hit discord bot known previously as Officer-Beepsky, now it goes simply by Beepsky
/// </summary>
public static class Program
{
    /// <summary>
    ///   Gets it going
    /// </summary>
    /// <param name="args">Arg, I'm a pirate</param>
    public static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Starting up Beepsky...");
            await Host.CreateApplicationBuilder(args).BuildHost().RunHostAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Beepsky failed to start up: " + ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }

    // Serilog cannot be used here, even after AddBeepskyLoggingConfiguration(), it only becomes available after builder.Build() is called.
    private static IHost BuildHost(this HostApplicationBuilder builder)
    {
        builder.Services.AddBeepskyLoggingConfiguration();
        BeepskyConfiguration config = builder.Configuration.AddBeepskyConfiguration();

        builder.ConfigureContainer(new AutofacServiceProviderFactory(), containerBuilder =>
        {
            containerBuilder.RegisterModule<BeepskyModule>();
            containerBuilder.RegisterModule(new BeepskyCoreModule(config));
        });

        builder.Services.AddDbContext<BeepskyDbContext>();
        builder.Services.AddBeepskyDiscordBot(config);
        builder.Services.AddBeepskyBackgroundServices();

        Console.WriteLine("Host built successfully...");
        return builder.Build();
    }

    private static async Task RunHostAsync(this IHost host)
    {
        // Text commands
        host.AddCommandModule<AudioCommandModule>();
        host.AddCommandModule<GeneralCommandModule>();
        host.AddCommandModule<SystemCommandModule>();

        // Slash commands
        // (none currently)

        ISender sender = host.Services.GetRequiredService<ISender>();
        await sender.Send(new MigrateDb.Command());

        Log.Information("Startup complete!");
        await host.RunAsync();
    }
}
