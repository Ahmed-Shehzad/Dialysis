using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.Scripts;
using Dialysis.SmartConnect.Transforms;
using Microsoft.Extensions.Configuration;
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
        services.TryAddSingleton<IConfiguration>(_ => new ConfigurationBuilder().Build());
        services.AddHttpClient("smartconnect-outbound");
        services.AddSingleton<AllowAllRouteFilter>();
        services.AddSingleton<PassThroughOutboundAdapter>();
        services.AddSingleton<HttpOutboundAdapter>();
        services.AddSingleton<FileOutboundAdapter>();
        services.AddSingleton<SmtpOutboundAdapter>();
        services.AddSingleton<TcpOutboundAdapter>();
        services.AddSingleton<ChannelWriterOutboundAdapter>();
        services.AddSingleton<JavascriptTransformStage>();
        services.AddSingleton<JavascriptRouteFilter>();
        services.AddSingleton<RuleBuilderRouteFilter>();
        services.AddSingleton<XsltTransformStage>();
        services.AddSingleton<JsonTransformStage>();
        services.AddSingleton<XmlTransformStage>();
        services.AddSingleton<MessageBuilderTransformStage>();
        services.AddSingleton<MapperTransformStage>(sp => new MapperTransformStage(sp.GetRequiredService<JsonTransformStage>()));
        services.TryAddSingleton<IDatabaseOutboundConnectionFactory, ConfigurationDatabaseOutboundConnectionFactory>();
        services.AddSingleton<DatabaseOutboundAdapter>();
        services.TryAddSingleton<IVariableMapStore, InMemoryVariableMapStore>();
        services.AddScoped<ChannelScriptExecutor>();
        services.AddSingleton<MutableFlowPluginRegistry>(sp =>
        {
            var registry = new MutableFlowPluginRegistry();
            registry.RegisterRouteFilter(sp.GetRequiredService<AllowAllRouteFilter>());
            registry.RegisterRouteFilter(sp.GetRequiredService<JavascriptRouteFilter>());
            registry.RegisterRouteFilter(sp.GetRequiredService<RuleBuilderRouteFilter>());
            registry.RegisterOutboundAdapter(sp.GetRequiredService<PassThroughOutboundAdapter>());
            registry.RegisterOutboundAdapter(sp.GetRequiredService<HttpOutboundAdapter>());
            registry.RegisterOutboundAdapter(sp.GetRequiredService<FileOutboundAdapter>());
            registry.RegisterOutboundAdapter(sp.GetRequiredService<SmtpOutboundAdapter>());
            registry.RegisterOutboundAdapter(sp.GetRequiredService<TcpOutboundAdapter>());
            registry.RegisterOutboundAdapter(sp.GetRequiredService<DatabaseOutboundAdapter>());
            registry.RegisterOutboundAdapter(sp.GetRequiredService<ChannelWriterOutboundAdapter>());
            registry.RegisterTransformStage(sp.GetRequiredService<JavascriptTransformStage>());
            registry.RegisterTransformStage(sp.GetRequiredService<XsltTransformStage>());
            registry.RegisterTransformStage(sp.GetRequiredService<JsonTransformStage>());
            registry.RegisterTransformStage(sp.GetRequiredService<XmlTransformStage>());
            registry.RegisterTransformStage(sp.GetRequiredService<MessageBuilderTransformStage>());
            registry.RegisterTransformStage(sp.GetRequiredService<MapperTransformStage>());
            return registry;
        });
        services.AddSingleton<IFlowPluginRegistry>(sp => sp.GetRequiredService<MutableFlowPluginRegistry>());
        services.AddScoped<IFlowRuntime, FlowRuntimeEngine>();

        return services;
    }

    /// <summary>Registers the background data pruner service with default or configured options.</summary>
    public static IServiceCollection AddSmartConnectDataPruner(this IServiceCollection services, Action<DataPrunerOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<DataPrunerOptions>(_ => { });

        services.AddHostedService<DataPrunerHostedService>();
        return services;
    }
}
