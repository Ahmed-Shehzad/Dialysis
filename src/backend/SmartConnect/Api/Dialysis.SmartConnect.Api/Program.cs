using Dialysis.SmartConnect;
using Dialysis.SmartConnect.Inbound;
using Dialysis.SmartConnect.Inbound.AspNetCore;
using Dialysis.SmartConnect.Inbound.Mllp;
using Dialysis.SmartConnect.Management.AspNetCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.InMemory;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSmartConnectPersistenceInMemory(databaseName: "SmartConnectApi");
builder.Services.AddSmartConnectCore();
builder.Services.AddSmartConnectDataPruner(
    o =>
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
builder.Services.AddSmartConnectManagementJwt(builder.Configuration);
builder.Services.AddHealthChecks();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(static r => r.AddService("Dialysis.SmartConnect.Api"))
    .WithTracing(static t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter())
    .WithMetrics(static m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

WebApplication app = builder.Build();

app.UseStaticFiles();

var jwtAuthority = app.Configuration["SmartConnect:Management:Jwt:Authority"];
if (!string.IsNullOrWhiteSpace(jwtAuthority))
{
    app.UseAuthentication();
}

app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/smartconnect/index.html", permanent: false));
app.MapSmartConnectInboundRoutes();
app.MapSmartConnectManagementRoutes();
app.MapSmartConnectGroupRoutes();
app.MapSmartConnectLedgerRoutes();
app.MapSmartConnectConfigurationMapRoutes();
app.MapSmartConnectEventsRoutes();
app.MapSmartConnectPrunerRoutes();

app.Run();

/// <summary>Marker for <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>.</summary>
public partial class Program;
