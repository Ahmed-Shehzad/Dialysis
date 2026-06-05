using Dialysis.BuildingBlocks.Fhir.Audit;
using Dialysis.BuildingBlocks.Hipaa;
using Dialysis.BuildingBlocks.Hipaa.AspNetCore;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.Module.Hosting;
using Dialysis.ServiceDefaults;
using Dialysis.SmartConnect;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Contracts.Security;
using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Inbound.AspNetCore;
using Dialysis.SmartConnect.Inbound.FileReader;
using Dialysis.SmartConnect.Inbound.Mllp;
using Dialysis.SmartConnect.Inbound.Sftp;
using Dialysis.SmartConnect.Inbound.Transponder;
using Dialysis.SmartConnect.Management.AspNetCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.AddModuleHost<SmartConnectPermissionCatalog>(new ModuleHostingOptions
{
    ModuleSlug = "smartconnect",
    HandlerAssemblies = [typeof(Dialysis.SmartConnect.Api.Program).Assembly],
});

builder.Services.AddSmartConnectPersistenceInMemory(databaseName: "SmartConnectApi");
builder.Services.AddSmartConnectCore();
builder.Services.AddSmartConnectDataPruner(o =>
{
    var hours = builder.Configuration.GetValue<double?>("SmartConnect:DataPruner:IntervalHours");
    if (hours is > 0)
        o.Interval = TimeSpan.FromHours(hours.Value);
    var days = builder.Configuration.GetValue<double?>("SmartConnect:DataPruner:RetentionDays");
    if (days is > 0)
        o.RetentionPeriod = TimeSpan.FromDays(days.Value);
});
builder.Services.AddDefaultInboundMessageFactory();
builder.Services.AddSmartConnectInboundTransport();
builder.Services.AddSmartConnectInboundHttpOptions();
builder.Services.AddSmartConnectMllpInbound();
builder.Services.AddSmartConnectFileReader();
builder.Services.AddSmartConnectSftpInbound();
builder.Services.AddSmartConnectTransponderInboundBridgeIfEnabled(builder.Configuration);
builder.Services.AddHostedService<BuiltInCodeTemplatesSeeder>();

// Transponder bus for the host: required by the transponder-bus outbound adapter, and carries the
// Lab result bridge that turns a routed inbound ORU into the Lab context's typed result event.
builder.Services.AddTransponder(t =>
    t.AddConsumer<Dialysis.SmartConnect.Contracts.Integration.SmartConnectRoutedPayloadIntegrationEvent,
        Dialysis.SmartConnect.Api.Lab.LabResultBridgeConsumer>());

if (builder.Configuration.GetValue("SmartConnect:Demo:Enabled", false))
    builder.Services.AddHostedService<Dialysis.SmartConnect.Api.Demo.SmartConnectDemoSeeder>();

if (builder.Configuration.GetValue("SmartConnect:Demo:Hl7Simulator", false))
    builder.Services.AddHostedService<Dialysis.SmartConnect.Api.Demo.Hl7V2SimulatorService>();

// HIPAA Security Rule scaffolding — see src/backend/HIS/README.md for the rationale.
builder.Services.AddFhirAudit();
builder.Services.AddHipaaCompliance("smartconnect");
builder.Services.AddHipaaAspNetCoreSafeguards();

var app = builder.Build();

app.UseStaticFiles();
app.UseModuleHost();

app.MapGet("/", () => Results.Redirect("/smartconnect/index.html", permanent: false));
app.MapSmartConnectInboundRoutes();
app.MapSmartConnectManagementRoutes();
app.MapSmartConnectWorkbenchRoutes();
app.MapHipaaSafeguardsEndpoint();
app.MapSmartConnectGroupRoutes();
app.MapSmartConnectLedgerRoutes();
app.MapSmartConnectConfigurationMapRoutes();
app.MapSmartConnectCodeTemplateLibraryRoutes();
app.MapSmartConnectAttachmentRoutes();
app.MapSmartConnectAlertRoutes();
app.MapSmartConnectEventsRoutes();
app.MapSmartConnectPrunerRoutes();

await app.RunAsync().ConfigureAwait(false);

namespace Dialysis.SmartConnect.Api
{
    /// <summary>Marker for <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.</summary>
    public partial class Program;
}
