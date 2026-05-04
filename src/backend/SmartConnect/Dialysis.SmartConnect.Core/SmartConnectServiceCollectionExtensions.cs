using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.ExtendedPlugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect;

public static class SmartConnectServiceCollectionExtensions
{
    /// <summary>
    /// Registers SmartConnect runtime services: <see cref="IFlowRuntime"/>, <see cref="IFlowPluginRegistry"/>, and built-in plugins.
    /// Call persistence plugins (for example <c>AddSmartConnectPersistenceInMemory</c> or <c>AddSmartConnectPersistenceForSqlServer</c>) to register <see cref="Persistence.IIntegrationFlowRepository"/> and <see cref="Persistence.IMessageLedger"/>.
    /// </summary>
    public static IServiceCollection AddSmartConnectCore(this IServiceCollection services)
    {
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddHttpClient("smartconnect-outbound");
        services.AddSingleton<AllowAllRouteFilter>();
        services.AddSingleton<PassThroughOutboundAdapter>();
        services.AddSingleton<HttpOutboundAdapter>();
        services.AddSingleton<FileOutboundAdapter>();
        services.AddSingleton<SmtpOutboundAdapter>();
        services.AddSingleton<JavascriptTransformStage>();
        services.AddSingleton<MutableFlowPluginRegistry>(sp =>
        {
            var registry = new MutableFlowPluginRegistry();
            registry.RegisterRouteFilter(sp.GetRequiredService<AllowAllRouteFilter>());
            registry.RegisterOutboundAdapter(sp.GetRequiredService<PassThroughOutboundAdapter>());
            registry.RegisterOutboundAdapter(sp.GetRequiredService<HttpOutboundAdapter>());
            registry.RegisterOutboundAdapter(sp.GetRequiredService<FileOutboundAdapter>());
            registry.RegisterOutboundAdapter(sp.GetRequiredService<SmtpOutboundAdapter>());
            registry.RegisterTransformStage(sp.GetRequiredService<JavascriptTransformStage>());
            return registry;
        });
        services.AddSingleton<IFlowPluginRegistry>(sp => sp.GetRequiredService<MutableFlowPluginRegistry>());
        services.AddScoped<IFlowRuntime, FlowRuntimeEngine>();

        return services;
    }
}
