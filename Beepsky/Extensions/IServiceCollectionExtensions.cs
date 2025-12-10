using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Beepsky.Extensions;

/// <summary>
///   Service collection extensions that should be used across the entire project.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    ///   Add the Serilog configuration to the service collection
    /// </summary>
    /// <param name="services"></param>
    public static void AddGlobalSerilogConfiguration(this IServiceCollection services)
    {
        // Specifically not reading this from appsettings, as ideally I'd like to get rid of them
        // since they don't really fit the 'linux' style of application configuration.
        services.AddSerilog(configure =>
            // TODO: Eventually this should be changed to not debug in production, but for now ¯\_(ツ)_/¯
            configure.MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
        );
    }

}
