using System.Globalization;
using Beepsky.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Beepsky.Core.Extensions;

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
    ///   Adds all of Beepskys background services to the service collection
    /// </summary>
    /// <param name="services"></param>
    public static void AddBeepskyBackgroundServices(this IServiceCollection services)
    {
        services.AddHostedService<AudioPlaybackService>();
        services.AddHostedService<AudioDownloadService>();
    }
}
