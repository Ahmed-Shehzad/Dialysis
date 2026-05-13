using Dialysis.Module.Hosting;
using Dialysis.SmartConnect;
using Dialysis.SmartConnect.CodeTemplates;
using Dialysis.SmartConnect.Contracts.Security;
using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Inbound.AspNetCore;
using Dialysis.SmartConnect.Inbound.Mllp;
using Dialysis.SmartConnect.Management.AspNetCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.AddModuleHost<SmartConnectPermissionCatalog>(new ModuleHostingOptions
{
    ModuleSlug = "smartconnect",
    HandlerAssemblies = new[] { typeof(Program).Assembly },
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
builder.Services.AddHostedService<BuiltInCodeTemplatesSeeder>();

var app = builder.Build();

app.UseStaticFiles();
app.UseModuleHost();

app.MapGet("/", () => Results.Redirect("/smartconnect/index.html", permanent: false));
app.MapSmartConnectInboundRoutes();
app.MapSmartConnectManagementRoutes();
app.MapSmartConnectGroupRoutes();
app.MapSmartConnectLedgerRoutes();
app.MapSmartConnectConfigurationMapRoutes();
app.MapSmartConnectCodeTemplateLibraryRoutes();
app.MapSmartConnectAttachmentRoutes();
app.MapSmartConnectAlertRoutes();
app.MapSmartConnectEventsRoutes();
app.MapSmartConnectPrunerRoutes();

await app.RunAsync().ConfigureAwait(false);

/// <summary>Marker for <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.</summary>
public partial class Program;
