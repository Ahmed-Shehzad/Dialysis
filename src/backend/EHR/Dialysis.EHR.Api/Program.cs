using Dialysis.BuildingBlocks.Transponder.Transport.RabbitMq;
using Dialysis.EHR.Billing;
using Dialysis.EHR.ClinicalNotes;
using Dialysis.EHR.Composition;
using Dialysis.EHR.Contracts.Integration;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.Integration;
using Dialysis.EHR.PatientChart;
using Dialysis.EHR.PatientPortal;
using Dialysis.EHR.Registration;
using Dialysis.EHR.Scheduling;
using Dialysis.Module.Hosting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

const string connectionStringName = "Ehr";
var connectionString = builder.Configuration.GetConnectionString(connectionStringName);
var enableOutbox = builder.Configuration.GetValue("Ehr:Transponder:EnableOutboxRelay", false);
var rabbitUri = builder.Configuration["Ehr:Transponder:RabbitMq:ConnectionUri"];
var rabbitQueue = builder.Configuration["Ehr:Transponder:RabbitMq:QueueName"];
var rabbitExchange = builder.Configuration["Ehr:Transponder:RabbitMq:ExchangeName"];

builder.AddModuleHost<EhrPermissionCatalog>(new ModuleHostingOptions
{
    ModuleSlug = "ehr",
    HandlerAssemblies = new[]
    {
        typeof(EhrRegistrationMarker).Assembly,
        typeof(EhrPatientChartMarker).Assembly,
        typeof(EhrSchedulingMarker).Assembly,
        typeof(EhrPatientPortalMarker).Assembly,
        typeof(EhrClinicalNotesMarker).Assembly,
        typeof(EhrBillingMarker).Assembly,
        typeof(EhrIntegrationMarker).Assembly,
    },
});

builder.Services.AddElectronicHealthRecord(
    builder.Configuration,
    configurePersistence: string.IsNullOrWhiteSpace(connectionString)
        ? null
        : options => options.UseNpgsql(
            connectionString,
            pg => pg.MigrationsHistoryTable("__ef_migrations", "ehr")),
    enableOutboxRelay: enableOutbox,
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
        }));

var app = builder.Build();

app.UseModuleHost();
app.MapOpenApi();

app.MapGet("/", () => Results.Ok(new { module = "ehr", version = "v1" }));

await app.RunAsync().ConfigureAwait(false);

/// <summary>Test factory marker.</summary>
public partial class Program;
