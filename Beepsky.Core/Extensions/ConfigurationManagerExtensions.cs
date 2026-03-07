using Beepsky.Core.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Beepsky.Core.Extensions;

/// <summary>
///   Extensions for the ConfigurationManager class
/// </summary>
public static class ConfigurationManagerExtensions
{
    /// <summary>
    ///   Adds the BeepskyConfiguration to the configuration builder
    /// </summary>
    /// <param name="configurationBuilder"></param>
    /// <returns></returns>
    public static BeepskyConfiguration AddBeepskyConfiguration(this ConfigurationManager configurationBuilder)
    {
        const string configFilePath = "/etc/beepsky/config.json";
        Console.WriteLine($"Loading configuration from: {configFilePath}");

        // Optional true here so we can return a more specific error message below, it is still required.
        configurationBuilder.AddJsonFile(configFilePath, optional: true, reloadOnChange: true);

        BeepskyConfiguration? config = configurationBuilder.GetSection("BeepskyConfiguration").Get<BeepskyConfiguration>();

        if (config is null)
        {
            throw new BeepskyException($"'BeepskyConfiguration' is missing from {configFilePath}");
        }

        if (string.IsNullOrWhiteSpace(config.DiscordBotToken))
        {
            throw new BeepskyException("'BeepskyConfiguration:DiscordBotToken' is not set in configuration.");
        }

        if (string.IsNullOrWhiteSpace(config.DatabaseConnectionString))
        {
            throw new BeepskyException("'BeepskyConfiguration:DatabaseConnectionString' is not set in configuration.");
        }

        if (string.IsNullOrWhiteSpace(config.DownloadCache))
        {
            throw new BeepskyException("'BeepskyConfiguration:DownloadCache' is not set in configuration.");
        }

        Console.WriteLine("Configuration loaded successfully");

        return config;
    }
}
