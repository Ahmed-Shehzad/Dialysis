using Dialysis.BuildingBlocks.Fhir.BulkData;
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
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

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

var app = builder.Build();

app.UseModuleHost();
app.MapOpenApi();

app.MapGet("/", () => Results.Ok(new { module = "pdms", version = "v1" }));
app.MapControllers();
app.MapHub<VitalsHub>(VitalsHub.Path);

if (enablePdmsBulkDataExport)
    app.MapFhirBulkDataEndpoints();

await app.RunAsync().ConfigureAwait(false);

namespace Dialysis.PDMS.Api
{
    public partial class Program;
}
