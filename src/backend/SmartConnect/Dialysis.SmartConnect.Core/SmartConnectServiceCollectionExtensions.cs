using Dialysis.SmartConnect.Alerts;
using Dialysis.SmartConnect.Alerts.Actions;
using Dialysis.SmartConnect.Attachments;
using Dialysis.SmartConnect.Attachments.Handlers;
using Dialysis.SmartConnect.Authentication;
using Dialysis.SmartConnect.BuiltInPlugins;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Endpoints;
using Dialysis.SmartConnect.ExtendedPlugins;
using Dialysis.SmartConnect.ExtendedPlugins.Authentication;
using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Routing;
using Dialysis.SmartConnect.Fhir;
using Dialysis.SmartConnect.Fhir.Mappers;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Scripts;
using Dialysis.SmartConnect.Ncpdp;
using Dialysis.SmartConnect.TimeSync;
using Dialysis.SmartConnect.Transforms;
using Dialysis.SmartConnect.VariableMaps;
using Hl7.Fhir.Model;
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
            // Slice J2: register the clock-skew monitor so the §2 probe (called from
            // inbound transports) actually accumulates observations the operator dashboard
            // reads. In-memory is fine for single-replica; swap for a Valkey-backed impl
            // when SmartConnect scales out.
            services.TryAddSingleton<IClockSkewMonitor, InMemoryClockSkewMonitor>();
            // Slice J3: clock-skew correction audit sink. The default no-op lets hosts run
            // the corrector without crashing; production hosts swap in a Transponder-backed
            // sink that publishes Hl7V2ClockSkewCorrectedIntegrationEvent.
            services.TryAddSingleton<IClockSkewCorrectionEventSink, NullClockSkewCorrectionEventSink>();
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
            // Slice A2: mutual TLS provider + the per-cert HttpClient pool it borrows from.
            services.TryAddSingleton<IMutualTlsHttpClientFactory, MutualTlsHttpClientFactory>();
            services.AddSingleton<IHttpAuthenticationProvider, MutualTlsAuthenticationProvider>();
            services.TryAddSingleton<IHttpAuthenticationProviderRegistry, HttpAuthenticationProviderRegistry>();

            services.AddSingleton<HttpOutboundAdapter>();
            services.AddSingleton<FileOutboundAdapter>();
            services.AddSingleton<SmtpOutboundAdapter>();
            services.AddSingleton<TcpOutboundAdapter>();
            services.AddSingleton<ChannelWriterOutboundAdapter>();
            services.AddSingleton<TransponderBusOutboundAdapter>();

            // Named-endpoint resolver — wired by default. Hosts that register an EF-backed
            // IEndpointRepository (via persistence composition) get name lookups; others get the
            // pass-through behaviour automatically because DefaultEndpointResolver returns the
            // input unchanged when the repository is not present.
            services.TryAddSingleton<IEndpointResolver, DefaultEndpointResolver>();

            // Content-based message router — source connectors can dispatch through this to fan a
            // single inbound message out to every Started flow whose InboundSubscriptions match.
            // Backwards compatible: source connectors that don't call the router are unchanged.
            services.TryAddSingleton<IMessageRouter, DefaultMessageRouter>();

            // HL7 v2 -> FHIR R4 mappers — auto-discovered by Hl7V2ToFhirPipeline via
            // IFhirV2MessageMapperWrapper. Each typed IFhirV2MessageMapper<TResource> is wrapped
            // so the pipeline can hold a heterogeneous collection across resource types.
            services.AddSingleton<AdtA01ToPatientMapper>();
            services.AddSingleton<AdtA01ToEncounterMapper>();
            services.AddSingleton<AdtA04ToPatientMapper>();
            services.AddSingleton<AdtA08ToPatientMapper>();
            services.AddSingleton<AdtA40ToPatientMapper>();
            services.AddSingleton<OruR01ToObservationMapper>();
            services.AddSingleton<OruR30ToObservationMapper>();
            services.AddSingleton<OruR40ToObservationMapper>();
            services.AddSingleton<OrmO01ToServiceRequestMapper>();
            services.AddSingleton<SiuS12ToAppointmentMapper>();
            services.AddSingleton<MdmT02ToDocumentReferenceMapper>();
            services.AddSingleton<VxuV04ToImmunizationMapper>();
            services.AddSingleton<IFhirV2MessageMapperWrapper>(sp => new FhirV2MessageMapperWrapper<Patient>(sp.GetRequiredService<AdtA01ToPatientMapper>()));
            services.AddSingleton<IFhirV2MessageMapperWrapper>(sp => new FhirV2MessageMapperWrapper<Encounter>(sp.GetRequiredService<AdtA01ToEncounterMapper>()));
            services.AddSingleton<IFhirV2MessageMapperWrapper>(sp => new FhirV2MessageMapperWrapper<Patient>(sp.GetRequiredService<AdtA04ToPatientMapper>()));
            services.AddSingleton<IFhirV2MessageMapperWrapper>(sp => new FhirV2MessageMapperWrapper<Patient>(sp.GetRequiredService<AdtA08ToPatientMapper>()));
            services.AddSingleton<IFhirV2MessageMapperWrapper>(sp => new FhirV2MessageMapperWrapper<Patient>(sp.GetRequiredService<AdtA40ToPatientMapper>()));
            services.AddSingleton<IFhirV2MessageMapperWrapper>(sp => new FhirV2MessageMapperWrapper<Observation>(sp.GetRequiredService<OruR01ToObservationMapper>()));
            services.AddSingleton<IFhirV2MessageMapperWrapper>(sp => new FhirV2MessageMapperWrapper<Observation>(sp.GetRequiredService<OruR30ToObservationMapper>()));
            services.AddSingleton<IFhirV2MessageMapperWrapper>(sp => new FhirV2MessageMapperWrapper<Observation>(sp.GetRequiredService<OruR40ToObservationMapper>()));
            services.AddSingleton<IFhirV2MessageMapperWrapper>(sp => new FhirV2MessageMapperWrapper<ServiceRequest>(sp.GetRequiredService<OrmO01ToServiceRequestMapper>()));
            services.AddSingleton<IFhirV2MessageMapperWrapper>(sp => new FhirV2MessageMapperWrapper<Appointment>(sp.GetRequiredService<SiuS12ToAppointmentMapper>()));
            services.AddSingleton<IFhirV2MessageMapperWrapper>(sp => new FhirV2MessageMapperWrapper<DocumentReference>(sp.GetRequiredService<MdmT02ToDocumentReferenceMapper>()));
            services.AddSingleton<IFhirV2MessageMapperWrapper>(sp => new FhirV2MessageMapperWrapper<Immunization>(sp.GetRequiredService<VxuV04ToImmunizationMapper>()));
            services.AddSingleton<Hl7V2ToFhirPipeline>();
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
            services.AddSingleton<NcpdpTelecomTransformStage>();
            // Slice K2: per-transaction NCPDP → FHIR mappers + dispatch stage.
            services.AddSingleton<INcpdpToFhirMapper, NcpdpBillingToClaimMapper>();
            services.AddSingleton<INcpdpToFhirMapper, NcpdpReversalToClaimMapper>();
            services.AddSingleton<INcpdpToFhirMapper, NcpdpEligibilityToCoverageEligibilityRequestMapper>();
            services.AddSingleton<INcpdpToFhirMapper, NcpdpInfoReportingToMedicationDispenseMapper>();
            services.AddSingleton<NcpdpToFhirTransformStage>();
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
                registry.RegisterOutboundAdapter(sp.GetRequiredService<TransponderBusOutboundAdapter>());
                registry.RegisterTransformStage(sp.GetRequiredService<JavascriptTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<ExternalScriptTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<XsltTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<JsonTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<XmlTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<DicomTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<DelimitedTextTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<NcpdpTelecomTransformStage>());
                registry.RegisterTransformStage(sp.GetRequiredService<NcpdpToFhirTransformStage>());
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
