using Autofac;
using Beepsky.Exceptions;
using Beepsky.PipelineBehaviors;
using Beepsky.Services;
using Questy.Autofac;
using Questy.Autofac.Builder;

namespace Beepsky;

/// <summary>
///   Core module for Beepsky
/// </summary>
public class BeepskyModule(BeepskyConfiguration config) : Module
{
    /// <inheritdoc />
    protected override void Load(ContainerBuilder builder)
    {
        if (string.IsNullOrWhiteSpace(config.DiscordBotToken))
        {
            throw new BeepskyException("Discord bot token is not set in configuration.");
        }

        if (string.IsNullOrWhiteSpace(config.DatabaseConnectionString))
        {
            throw new BeepskyException("Database connection string is not set in configuration.");
        }

        if (string.IsNullOrWhiteSpace(config.DownloadCache))
        {
            throw new BeepskyException("Download cache directory is not set in configuration.");
        }

        builder.RegisterInstance(config)
               .AsSelf()
               .SingleInstance();

        builder.RegisterType<AudioQueueService>()
               .AsSelf()
               .SingleInstance();

        builder.RegisterType<VoiceConnectionService>()
               .AsSelf()
               .SingleInstance();

        QuestyConfigurationBuilder questyConfig = QuestyConfigurationBuilder.Create(ThisAssembly)
            .WithAllOpenGenericHandlerTypesRegistered()
            .WithCustomPipelineBehaviors([
                typeof(CommandBehavior<>),
                typeof(QueryBehavior<,>)
            ]);

        builder.RegisterQuesty(questyConfig.Build());
    }
}
