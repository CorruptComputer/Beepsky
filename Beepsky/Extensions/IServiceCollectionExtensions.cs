using System.Globalization;
using Beepsky.DiscordEventHandlers;
using Beepsky.Services;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.Commands;
using Serilog;

namespace Beepsky.Extensions;

/// <summary>
///   Service collection extensions that should be used across the entire project.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    ///   Add the logging configuration to the service collection
    /// </summary>
    /// <param name="services"></param>
    public static void AddBeepskyLoggingConfiguration(this IServiceCollection services)
    {
        // Specifically not reading this from appsettings, as ideally I'd like to get rid of them
        // since they don't really fit the 'linux' style of application configuration.
        services.AddSerilog(configure =>
#if DEBUG
            configure.MinimumLevel.Debug()
#else
            configure.MinimumLevel.Information()
#endif
                .Enrich.FromLogContext()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
        );
    }

    /// <summary>
    ///   Adds all of Beepskys discord bot related services to the service collection
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    public static void AddBeepskyDiscordBot(this IServiceCollection services, BeepskyConfiguration config)
    {
        services.AddDiscordGateway(options =>
        {
            options.Token = config.DiscordBotToken;
            options.Intents = NetCord.Gateway.GatewayIntents.All;
        })
        .AddGatewayHandler<BeepskyReplyMessageCreateHandler>()
        .AddGatewayHandler<GuildUserStatisticMessageCreateHandler>()
        .AddGatewayHandler<VoiceStateUpdateHandler>()
        .AddGatewayHandler<GatewayConnectedHandler>()
        .AddCommands(options =>
        {
            options.Prefix = new(BeepskyConfiguration.Prefix, 1);
            options.IgnoreCase = true;
        });
    }

    /// <summary>
    ///   Adds all of Beepskys background services to the service collection
    /// </summary>
    /// <param name="services"></param>
    public static void AddBeepskyBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<AudioPlaybackService>();
        services.AddHostedService<AudioDownloadService>();
    }
}
