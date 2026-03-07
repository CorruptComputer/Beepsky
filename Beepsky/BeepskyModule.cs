using Autofac;
using Beepsky.Core.Services;
using Beepsky.Services;

namespace Beepsky;

/// <summary>
///   Autofac module for Beepsky
/// </summary>
public class BeepskyModule : Module
{
    /// <inheritdoc />
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<DiscordIntegrationService>()
               .As<IDiscordIntegrationService>()
               .SingleInstance();
    }
}
