using Autofac;
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
