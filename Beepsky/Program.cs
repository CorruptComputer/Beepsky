using Autofac;
using Autofac.Extensions.DependencyInjection;
using Beepsky.Extensions;
using Beepsky.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using NetCord.Hosting.Gateway;
using Beepsky.DiscordEventHandlers;
using NetCord.Hosting.Services.Commands;
using Beepsky.Features.Commands.Text;
using Beepsky.Services;
using Beepsky.Database.Operations;

namespace Beepsky;

/// <summary>
///   Idk its a thing and it does stuff
/// </summary>
public static class Program
{
    /// <summary>
    ///   Gets it going
    /// </summary>
    /// <param name="args"></param>
    public static async Task Main(string[] args)
    {
        await Host.CreateApplicationBuilder(args).BuildHost().RunHost();
    }

    private static IHost BuildHost(this HostApplicationBuilder builder)
    {
        builder.Services.AddGlobalSerilogConfiguration();
        BeepskyConfiguration config = builder.Configuration.AddBeepskyConfiguration(builder.Environment);

        builder.ConfigureContainer(new AutofacServiceProviderFactory(), containerBuilder =>
        {
            containerBuilder.RegisterModule(new BeepskyModule(config));
        });

        builder.Services.AddDiscordGateway(options =>
        {
            options.Token = config.DiscordBotToken;
            options.Intents = NetCord.Gateway.GatewayIntents.All;
        })
        .AddGatewayHandler<MessageCreateHandler>()
        .AddCommands(options =>
        {
            options.Prefix = new(BeepskyConfiguration.Prefix, 1);
        });

        builder.Services.AddDbContext<BeepskyDbContext>(ServiceLifetime.Transient);
        builder.Services.AddHostedService<AudioPlaybackService>();
        builder.Services.AddHostedService<AudioDownloadService>();

        return builder.Build();
    }

    private static async Task RunHost(this IHost host)
    {
        Log.Information("Startup complete");
        host.AddCommandModule<AudioCommandModule>();
        host.AddCommandModule<GeneralCommandModule>();
        host.AddCommandModule<SystemCommandModule>();

        ISender sender = host.Services.GetRequiredService<ISender>();
        await sender.Send(new MigrateDb.Command());

        await host.RunAsync();
    }
}
