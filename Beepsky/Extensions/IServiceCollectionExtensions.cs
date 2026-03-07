using Beepsky.DiscordEventHandlers;
using Beepsky.DiscordEventHandlers.Commands;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services.Commands;
using NetCord.Services.Commands;

namespace Beepsky.Extensions;

/// <summary>
///   Service collection extensions that should be used across the entire project.
/// </summary>
public static class IServiceCollectionExtensions
{
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
            options.ResultHandler = new BeepskyCommandResultHandler<CommandContext>();
        });
    }
}
