using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Alerts.Actions;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Attachments.Handlers;
using Dialysis.SmartConnect.Authentication;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.ExtendedPlugins.Authentication;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Scripts;
using Dialysis.SmartConnect.Transforms;
using Dialysis.SmartConnect.VariableMaps;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect;

public static class SmartConnectServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers SmartConnect runtime services: <see cref="IFlowRuntime"/>, <see cref="IFlowPluginRegistry"/>, and built-in plugins.
        /// Call persistence plugins (for example <c>AddSmartConnectPersistenceInMemory</c> or <c>AddSmartConnectPersistenceForSqlServer</c>) to register <see cref="IIntegrationFlowRepository"/> and <see cref="IMessageLedger"/>.
        /// </summary>
        public IServiceCollection AddSmartConnectCore()
        {
            services.TryAddSingleton<TimeProvider>(TimeProvider.System);
            services.TryAddSingleton<IConfiguration>(_ => new ConfigurationBuilder().Build());
            services.AddHttpClient("smartconnect-outbound");
            services.AddSingleton<AllowAllRouteFilter>();
            services.AddSingleton<PassThroughOutboundAdapter>();

            // HTTP authentication providers (Bearer / API-Key / Basic / OAuth2 client-credentials).
            // Falls back to an in-memory IDistributedCache when the host hasn't wired Valkey/Redis;
            // a host that registers AddStackExchangeRedisCache first wins this TryAdd.
            services.AddDistributedMemoryCache();
            services.AddSingleton<IHttpAuthenticationProvider, BearerTokenAuthenticationProvider>();
            services.AddSingleton<IHttpAuthenticationProvider, ApiKeyAuthenticationProvider>();
            services.AddSingleton<IHttpAuthenticationProvider, BasicAuthenticationProvider>();
            services.AddSingleton<IHttpAuthenticationProvider, OAuth2ClientCredentialsAuthenticationProvider>();
            services.TryAddSingleton<IHttpAuthenticationProviderRegistry, HttpAuthenticationProviderRegistry>();

            services.AddSingleton<HttpOutboundAdapter>();
            services.AddSingleton<FileOutboundAdapter>();
            services.AddSingleton<SmtpOutboundAdapter>();
            services.AddSingleton<TcpOutboundAdapter>();
            services.AddSingleton<ChannelWriterOutboundAdapter>();
            services.TryAddSingleton<IFlowExecutionContextAccessor, FlowExecutionContextAccessor>();
            services.AddSingleton<JavascriptTransformStage>(sp => new JavascriptTransformStage(sp));
            services.AddSingleton<JavascriptRouteFilter>(sp => new JavascriptRouteFilter(sp));
            services.TryAddSingleton<IExternalScriptLoader, DefaultExternalScriptLoader>();
            services.AddOptions<ExternalScriptOptions>();
            services.AddSingleton<ExternalScriptRouteFilter>(sp =>
                new ExternalScriptRouteFilter(sp.GetRequiredService<IExternalScriptLoader>(), sp));
            services.AddSingleton<ExternalScriptTransformStage>(sp =>
                new ExternalScriptTransformStage(sp.GetRequiredService<IExternalScriptLoader>(), sp));
            services.AddSingleton<RuleBuilderRouteFilter>();
            services.AddSingleton<XsltTransformStage>();
            services.AddSingleton<JsonTransformStage>();
            services.AddSingleton<XmlTransformStage>();
            services.AddSingleton<DicomTransformStage>();
            services.AddSingleton<DelimitedTextTransformStage>();
            services.AddSingleton<MessageBuilderTransformStage>();
            services.AddSingleton<MapperTransformStage>(sp => new MapperTransformStage(sp.GetRequiredService<JsonTransformStage>()));
            services.AddSingleton<IteratorRouteFilter>(sp => new IteratorRouteFilter(sp));
            services.AddSingleton<IteratorTransformStage>(sp => new IteratorTransformStage(sp));
            services.AddSingleton<DestinationSetFilterTransformStage>();
            services.TryAddSingleton<IDatabaseOutboundConnectionFactory, ConfigurationDatabaseOutboundConnectionFactory>();
            services.AddSingleton<DatabaseOutboundAdapter>();
            services.TryAddSingleton<IVariableMapStore, InMemoryVariableMapStore>();
            services.AddScoped<ChannelScriptExecutor>();
            services.AddScoped<CodeTemplateLinkageService>();
            services.AddSingleton<MirthXmlCodeTemplateImporter>();

            // Attachment handlers + runtime services
            services.AddSingleton<NoneAttachmentHandler>();
            services.AddSingleton<EntireMessageAttachmentHandler>();
            services.AddSingleton<RegexAttachmentHandler>();
            services.AddSingleton<DicomAttachmentHandler>();
            services.AddSingleton<JavaScriptAttachmentHandler>(sp => new JavaScriptAttachmentHandler(sp));
            services.AddSingleton<CustomAttachmentHandlerHost>(sp =>
                new CustomAttachmentHandlerHost(sp.GetRequiredService<IFlowPluginRegistry>));
            services.AddScoped<AttachmentExtractionPipeline>();
            services.AddScoped<AttachmentReattachmentService>();

            // Alerts: 3 action providers + engine + sink
            services.AddSingleton<EmailAlertActionProvider>();
            services.AddSingleton<WebhookAlertActionProvider>();
            services.AddSingleton<ChannelRedispatchAlertActionProvider>(sp =>
                new ChannelRedispatchAlertActionProvider(
                    () => sp.CreateScope().ServiceProvider.GetRequiredService<IFlowRuntime>(),
                    sp.GetRequiredService<TimeProvider>()));
            services.AddScoped<AlertEngine>();
            services.AddScoped<IAlertSink>(sp => sp.GetRequiredService<AlertEngine>());
            services.AddSingleton<MutableFlowPluginRegistry>(sp =>
            {
                var registry = new MutableFlowPluginRegistry();
                registry.RegisterRouteFilter(sp.GetRequiredService<AllowAllRouteFilter>());
                registry.RegisterRouteFilter(sp.GetRequiredService<JavascriptRouteFilter>());
                registry.RegisterRouteFilter(sp.GetRequiredService<ExternalScriptRouteFilter>());
                registry.RegisterRouteFilter(sp.GetRequiredService<RuleBuilderRouteFilter>());
                registry.RegisterRouteFilter(sp.GetRequiredService<IteratorRouteFilter>());
                registry.RegisterOutboundAdapter(sp.GetRequiredService<PassThroughOutboundAdapter>());
                registry.RegisterOutboundAdapter(sp.GetRequiredService<HttpOutboundAdapter>());
                registry.RegisterOutboundAdapter(sp.GetRequiredService<FileOutboundAdapter>());
                registry.RegisterOutboundAdapter(sp.GetRequiredService<SmtpOutboundAdapter>());
                registry.RegisterOutboundAdapter(sp.GetRequiredService<TcpOutboundAdapter>());
                registry.RegisterOutboundAdapter(sp.GetRequiredService<DatabaseOutboundAdapter>());
                registry.RegisterOutboundAdapter(sp.GetRequiredService<ChannelWriterOutboundAdapter>());
                registry.RegisterTransformStage(sp.GetRequiredService<JavascriptTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<ExternalScriptTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<XsltTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<JsonTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<XmlTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<DicomTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<DelimitedTextTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<MessageBuilderTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<MapperTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<IteratorTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<DestinationSetFilterTransformStage>());
                registry.RegisterAttachmentHandler(sp.GetRequiredService<NoneAttachmentHandler>());
                registry.RegisterAttachmentHandler(sp.GetRequiredService<EntireMessageAttachmentHandler>());
                registry.RegisterAttachmentHandler(sp.GetRequiredService<RegexAttachmentHandler>());
                registry.RegisterAttachmentHandler(sp.GetRequiredService<DicomAttachmentHandler>());
                registry.RegisterAttachmentHandler(sp.GetRequiredService<JavaScriptAttachmentHandler>());
                registry.RegisterAttachmentHandler(sp.GetRequiredService<CustomAttachmentHandlerHost>());
                registry.RegisterAlertActionProvider(sp.GetRequiredService<EmailAlertActionProvider>());
                registry.RegisterAlertActionProvider(sp.GetRequiredService<WebhookAlertActionProvider>());
                registry.RegisterAlertActionProvider(sp.GetRequiredService<ChannelRedispatchAlertActionProvider>());
                return registry;
            });
            services.AddSingleton<IFlowPluginRegistry>(sp => sp.GetRequiredService<MutableFlowPluginRegistry>());
            services.AddScoped<IFlowRuntime, FlowRuntimeEngine>();

            return services;
        }
        /// <summary>Registers the background data pruner service with default or configured options.</summary>
        public IServiceCollection AddSmartConnectDataPruner(Action<DataPrunerOptions>? configure = null)
        {
            if (configure is not null)
                services.Configure(configure);
            else
                services.Configure<DataPrunerOptions>(_ => { });

            services.AddHostedService<DataPrunerHostedService>();
            return services;
        }
    }
}
