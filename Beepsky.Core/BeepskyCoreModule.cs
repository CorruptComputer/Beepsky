using Autofac;
using Beepsky.Core.PipelineBehaviors;
using Beepsky.Core.Services;
using Questy.Autofac;
using Questy.Autofac.Builder;

namespace Beepsky.Core;

/// <summary>
///   Autofac module for Beepsky.Core
/// </summary>
public class BeepskyCoreModule(BeepskyConfiguration config) : Module
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

        builder.RegisterType<LLMService>()
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
