using Dialysis.BuildingBlocks.DataProtection.AspNetCore;
using Dialysis.BuildingBlocks.DurableCommandBus;
using Dialysis.BuildingBlocks.DurableCommandBus.AspNetCore;
using Dialysis.BuildingBlocks.Fhir.Audit;
using Dialysis.BuildingBlocks.Fhir.BulkData;
using Dialysis.BuildingBlocks.Fhir.Smart;
using Dialysis.BuildingBlocks.Fhir.Subscriptions;
using Dialysis.BuildingBlocks.Hipaa;
using Dialysis.BuildingBlocks.Hipaa.AspNetCore;
using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.EHR.Billing;
using Dialysis.EHR.ClinicalNotes;
using Dialysis.EHR.Composition;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.Integration;
using Dialysis.EHR.PatientChart;
using Dialysis.EHR.PatientChart.Features.RecordAllergy;
using Dialysis.EHR.PatientChart.Features.RecordVitalSign;
using Dialysis.EHR.PatientPortal;
using Dialysis.EHR.Persistence;
using Dialysis.EHR.Registration;
using Dialysis.EHR.Scheduling;
using Dialysis.Module.Hosting;
using Dialysis.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

const string connectionStringName = "Ehr";
var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
var enableOutbox = builder.Configuration.GetValue("Ehr:Transponder:EnableOutboxRelay", false);
var rabbitUri = builder.Configuration["Ehr:Transponder:RabbitMq:ConnectionUri"];
var rabbitQueue = builder.Configuration["Ehr:Transponder:RabbitMq:QueueName"];
var rabbitExchange = builder.Configuration["Ehr:Transponder:RabbitMq:ExchangeName"];

builder.AddModuleHost<EhrPermissionCatalog>(new ModuleHostingOptions
{
    ModuleSlug = "ehr",
    HandlerAssemblies =
    [
        typeof(EhrRegistrationMarker).Assembly,
        typeof(EhrPatientChartMarker).Assembly,
        typeof(EhrSchedulingMarker).Assembly,
        typeof(EhrPatientPortalMarker).Assembly,
        typeof(EhrClinicalNotesMarker).Assembly,
        typeof(EhrBillingMarker).Assembly,
        typeof(EhrIntegrationMarker).Assembly
    ],
});

var enableEhrDemoSeed = builder.Configuration.GetValue("Ehr:Demo:Enabled", false);
var enableEhrRegistrationSim = builder.Configuration.GetValue("Ehr:Demo:RegistrationSimulator", false);
var enableEhrBulkDataExport = builder.Configuration.GetValue("Ehr:Fhir:BulkData:Enabled", false);
var enableEhrSmartOnFhir = builder.Configuration.GetValue("Ehr:Fhir:Smart:Enabled", false);
var enableEhrSubscriptions = builder.Configuration.GetValue("Ehr:Fhir:Subscriptions:Enabled", false);
// Persistence defaults to the feature flag; set false to use the in-memory registry
// (no DB / migration dependency — handy for local dev and demos).
var enableEhrSubscriptionsPersistence =
    builder.Configuration.GetValue("Ehr:Fhir:Subscriptions:Persistence", enableEhrSubscriptions);
var ehrBulkDataExportScope = builder.Configuration["Ehr:Fhir:BulkData:RequireScope"]
    ?? (enableEhrSmartOnFhir ? "system/*.read" : null);
var ehrSubscriptionsScope = builder.Configuration["Ehr:Fhir:Subscriptions:RequireScope"]
    ?? (enableEhrSmartOnFhir ? "user/*.write" : null);

builder.Services.AddElectronicHealthRecord(
    builder.Configuration,
    configurePersistence: string.IsNullOrWhiteSpace(connectionString)
        ? null
        : options => options.UseNpgsql(
            connectionString,
            pg => pg.MigrationsHistoryTable("__ef_migrations", "ehr")),
    enableOutboxRelay: enableOutbox,
    enableFhirBulkDataPersistence: enableEhrBulkDataExport,
    enableFhirBulkDataExport: enableEhrBulkDataExport,
    enableFhirSmartOnFhir: enableEhrSmartOnFhir,
    enableFhirSubscriptions: enableEhrSubscriptions,
    enableFhirSubscriptionsPersistence: enableEhrSubscriptionsPersistence,
    enableDemoSeed: enableEhrDemoSeed,
    enableRegistrationSimulator: enableEhrRegistrationSim,
    configureTransponderTransport: string.IsNullOrWhiteSpace(rabbitUri)
        ? null
        : s => s.AddTransponderRabbitMq(o =>
        {
            o.ConnectionUri = rabbitUri;
            if (!string.IsNullOrWhiteSpace(rabbitQueue)) o.QueueName = rabbitQueue;
            if (!string.IsNullOrWhiteSpace(rabbitExchange)) o.ExchangeName = rabbitExchange;
        },
        sub =>
        {
            // Subscribe to integration events we react to from other modules / our own outbox.
            sub.Listen<PrescriptionOrderedIntegrationEvent>();
            sub.Listen<LabOrderPlacedIntegrationEvent>();
            sub.Listen<ClaimSubmittedIntegrationEvent>();
            if (enableEhrSubscriptions)
                sub.Listen<LabResultReceivedIntegrationEvent>();
        }));

// HIPAA Security Rule scaffolding — see src/backend/HIS/README.md for the rationale.
builder.Services.AddFhirAudit();
builder.Services.AddHipaaCompliance("ehr");
builder.Services.AddHipaaAspNetCoreSafeguards();

// Durable command bus — third opt-in module (after PDMS PR #140, HIS PR #141).
// RecordVitalSign and RecordAllergy are the EHR chart writes most worth durable
// buffering: vitals are high-volume, allergies are clinically critical. Flag off
// by default; flip per env once production traffic patterns settle.
builder.Services.AddDurableCommandBus<EhrDbContext>("ehr", b =>
{
    b.RegisterCommand<RecordVitalSignCommand, Guid>(requiredPermission: EhrPermissions.VitalsRecord);
    b.RegisterCommand<RecordAllergyCommand, Guid>(requiredPermission: EhrPermissions.AllergyRecord);
});
builder.Services.Configure<Dialysis.Module.Hosting.Telemetry.ModuleTelemetryOptions>(o =>
    o.AdditionalMeters.Add(DurableCommandMetrics.MeterName));

builder.Services
    .AddControllers()
    // Accept string-named enum payloads from the SPA (consistent with PDMS/SmartConnect);
    // otherwise System.Text.Json only binds the integer backing values and rejects names with 400.
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

app.UseModuleHost();
if (enableEhrSubscriptions)
    app.UseWebSockets();
app.MapOpenApi();

app.MapGet("/", () => Results.Ok(new { module = "ehr", version = "v1" }));
app.MapHipaaSafeguardsEndpoint();
// GDPR / BDSG surface — RoPA + data-subject-rights. The SPA admin pages proxy here
// (/api/ehr/admin/data-protection/ropa, /api/ehr/api/v1.0/data-subject-rights/...).
app.MapEuDataProtectionRoutes();
app.MapDurableCommandStatusEndpoint();
app.MapControllers();

if (enableEhrSmartOnFhir)
    app.MapSmartConfigurationEndpoint();

if (enableEhrBulkDataExport)
    app.MapFhirBulkDataEndpoints(requireScope: ehrBulkDataExportScope);

if (enableEhrSubscriptions)
    app.MapFhirSubscriptionEndpoints(requireScope: ehrSubscriptionsScope);

await app.RunAsync().ConfigureAwait(false);

namespace Dialysis.EHR.Api
{
    /// <summary>Test factory marker.</summary>
    public partial class Program;
}
