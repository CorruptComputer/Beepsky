using Beepsky.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Beepsky.Extensions;

/// <summary>
///   Extensions for the ConfigurationManager class
/// </summary>
public static class ConfigurationManagerExtensions
{
    /// <summary>
    ///   Adds the BeepskyConfiguration to the configuration builder
    /// </summary>
    /// <param name="configurationBuilder"></param>
    /// <param name="environment"></param>
    /// <returns></returns>
    public static BeepskyConfiguration AddBeepskyConfiguration(this ConfigurationManager configurationBuilder, IHostEnvironment environment)
    {
        string configFilePath = "/etc/beepsky/config.json";

        Console.WriteLine($"Loading configuration from: {configFilePath}");

        Console.WriteLine("Configuration: \n" + File.ReadAllText(configFilePath));

        configurationBuilder.AddJsonFile(configFilePath, optional: false, reloadOnChange: true);
        configurationBuilder.AddEnvironmentVariables();

        BeepskyConfiguration? backendConfig = configurationBuilder.GetSection("BeepskyConfiguration").Get<BeepskyConfiguration>();

        if (backendConfig is null)
        {
            Console.WriteLine($"BeepskyConfiguration is missing from {configFilePath}");
            throw new BeepskyException($"BeepskyConfiguration is missing from {configFilePath}");
        }

        Console.WriteLine("Configuration loaded successfully");

        return backendConfig;
    }
}
