using Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;
using Dialysis.BuildingBlocks.Fhir.Audit;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.BuildingBlocks.Fhir.Smart;
using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Hipaa;
using Dialysis.BuildingBlocks.Hipaa.AspNetCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.Module.Hosting;
using Dialysis.PDMS.Api.Realtime;
using Dialysis.PDMS.Composition;
using Dialysis.PDMS.Contracts.Security;
using Dialysis.PDMS.TreatmentSessions;
using Dialysis.PDMS.TreatmentSessions.Realtime;
using Dialysis.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

const string connectionStringName = "Pdms";
var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
var enableOutbox = builder.Configuration.GetValue("Pdms:Transponder:EnableOutboxRelay", false);
var rabbitUri = builder.Configuration["Pdms:Transponder:RabbitMq:ConnectionUri"];
var rabbitQueue = builder.Configuration["Pdms:Transponder:RabbitMq:QueueName"];
var rabbitExchange = builder.Configuration["Pdms:Transponder:RabbitMq:ExchangeName"];

builder.AddModuleHost<PdmsPermissionCatalog>(new ModuleHostingOptions
{
    ModuleSlug = "pdms",
    HandlerAssemblies = [typeof(PdmsTreatmentSessionsMarker).Assembly],
});

var enablePdmsDemoSeed = builder.Configuration.GetValue("Pdms:Demo:Enabled", false);
var enablePdmsVitalsTicker = builder.Configuration.GetValue("Pdms:Demo:VitalsTicker", false);
var enablePdmsMachineSim = builder.Configuration.GetValue("Pdms:Demo:MachineTelemetrySimulator", false);
var enablePdmsBulkDataExport = builder.Configuration.GetValue("Pdms:Fhir:BulkData:Enabled", false);
var enablePdmsSmartOnFhir = builder.Configuration.GetValue("Pdms:Fhir:Smart:Enabled", false);
var enablePdmsSubscriptions = builder.Configuration.GetValue("Pdms:Fhir:Subscriptions:Enabled", false);
// Persistence defaults to the feature flag; set false to use the in-memory registry
// (no DB / migration dependency — handy for local dev and demos).
var enablePdmsSubscriptionsPersistence =
    builder.Configuration.GetValue("Pdms:Fhir:Subscriptions:Persistence", enablePdmsSubscriptions);
var pdmsBulkDataExportScope = builder.Configuration["Pdms:Fhir:BulkData:RequireScope"]
    ?? (enablePdmsSmartOnFhir ? "system/*.read" : null);
var pdmsSubscriptionsScope = builder.Configuration["Pdms:Fhir:Subscriptions:RequireScope"]
    ?? (enablePdmsSmartOnFhir ? "user/*.write" : null);

builder.Services.AddPatientDataManagementSystem(
    builder.Configuration,
    configurePersistence: string.IsNullOrWhiteSpace(connectionString)
        ? null
        : options => options.UseNpgsql(
            connectionString,
            pg => pg.MigrationsHistoryTable("__ef_migrations", "pdms")),
    enableOutboxRelay: enableOutbox,
    enableFhirBulkDataPersistence: enablePdmsBulkDataExport,
    enableFhirBulkDataExport: enablePdmsBulkDataExport,
    enableFhirSmartOnFhir: enablePdmsSmartOnFhir,
    enableFhirSubscriptions: enablePdmsSubscriptions,
    enableFhirSubscriptionsPersistence: enablePdmsSubscriptionsPersistence,
    enableDemoSeed: enablePdmsDemoSeed,
    enableVitalsTicker: enablePdmsVitalsTicker,
    enableMachineTelemetrySimulator: enablePdmsMachineSim,
    configureTransponderTransport: string.IsNullOrWhiteSpace(rabbitUri)
        ? null
        : s => s.AddTransponderRabbitMq(o =>
        {
            o.ConnectionUri = rabbitUri;
            if (!string.IsNullOrWhiteSpace(rabbitQueue)) o.QueueName = rabbitQueue;
            if (!string.IsNullOrWhiteSpace(rabbitExchange)) o.ExchangeName = rabbitExchange;
        }));

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()))
    // Emit a FHIR AuditEvent for every [PhiAccess]-tagged action — closes the audit-trail
    // loop the GDPR / BDSG / PDSG envelope cites.
    .AddPhiAccessAuditing();

// SignalR with optional Valkey/Redis backplane for horizontal scale-out. With the backplane,
// every PDMS replica subscribes to the same Valkey pub/sub channel, so a reading broadcast on
// replica A is delivered to clients connected to replica B in the same group. Without the
// backplane (no Valkey config), SignalR runs in-process and only fans out within one replica.
var signalRBuilder = builder.Services.AddSignalR(o =>
{
    // 1s broadcast cadence is tight — keep the keep-alive shorter than client default (15s)
    // so dropped connections fail fast and the reconnect handshake kicks in.
    o.KeepAliveInterval = TimeSpan.FromSeconds(5);
    o.ClientTimeoutInterval = TimeSpan.FromSeconds(15);
});
var valkeyConnectionString = builder.Configuration["Pdms:DistributedCache:Valkey:ConnectionString"];
if (!string.IsNullOrWhiteSpace(valkeyConnectionString))
{
    signalRBuilder.AddStackExchangeRedis(valkeyConnectionString, o =>
    {
        // Channel prefix isolates PDMS pub/sub traffic from other modules that may share Valkey.
        o.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("pdms-signalr");
    });
}

// Replace the default no-op broadcaster with the SignalR-backed implementation hosted alongside the hub.
builder.Services.AddSingleton<IVitalsBroadcaster, SignalRVitalsBroadcaster>();

// HIPAA Security Rule scaffolding — see src/backend/HIS/README.md for the rationale.
builder.Services.AddFhirAudit();
builder.Services.AddHipaaCompliance("pdms");
builder.Services.AddHipaaAspNetCoreSafeguards();

var app = builder.Build();

app.UseModuleHost();
if (enablePdmsSubscriptions)
    app.UseWebSockets();
app.MapOpenApi();

app.MapGet("/", () => Results.Ok(new { module = "pdms", version = "v1" }));
app.MapHipaaSafeguardsEndpoint();
app.MapControllers();
app.MapHub<VitalsHub>(VitalsHub.Path);

if (enablePdmsSmartOnFhir)
    app.MapSmartConfigurationEndpoint();

if (enablePdmsBulkDataExport)
    app.MapFhirBulkDataEndpoints(requireScope: pdmsBulkDataExportScope);

if (enablePdmsSubscriptions)
    app.MapFhirSubscriptionEndpoints(requireScope: pdmsSubscriptionsScope);

await app.RunAsync().ConfigureAwait(false);

namespace Dialysis.PDMS.Api
{
    public partial class Program;
}
