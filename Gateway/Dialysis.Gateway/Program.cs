using Microsoft.AspNetCore.Diagnostics.HealthChecks;

using BuildingBlocks.Logging;
using BuildingBlocks.TimeSync;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, config) =>
    config.ReadFrom.Configuration(context.Configuration)
        .Enrich.With<ActivityEnricher>()
        .Enrich.FromLogContext());

_ = builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Dialysis.Gateway"))
    .WithTracing(t =>
    {
        _ = t.AddAspNetCoreInstrumentation();
        _ = t.AddHttpClientInstrumentation();
    })
    .WithMetrics(m =>
    {
        _ = m.AddAspNetCoreInstrumentation();
        _ = m.AddPrometheusExporter();
    });

// Bind to 0.0.0.0 so Docker port mapping works; localhost-only causes "connection reset by peer"
builder.WebHost.UseUrls(
    builder.Configuration["ASPNETCORE_URLS"] ?? builder.Configuration["Urls"] ?? "http://0.0.0.0:5000");

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

string[] corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (corsOrigins.Length > 0)
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            _ = policy.WithOrigins(corsOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("X-Tenant-Id");
        });
    });

builder.Services.AddOpenApi();

string GetBackendAddress(string cluster, string destination, string @default) =>
    builder.Configuration[$"ReverseProxy:Clusters:{cluster}:Destinations:{destination}:Address"] ?? @default;

string patientUrl = GetBackendAddress("patient-cluster", "patient", "http://localhost:5051");
string prescriptionUrl = GetBackendAddress("prescription-cluster", "prescription", "http://localhost:5052");
string treatmentUrl = GetBackendAddress("treatment-cluster", "treatment", "http://localhost:5050");
string alarmUrl = GetBackendAddress("alarm-cluster", "alarm", "http://localhost:5053");
string deviceUrl = GetBackendAddress("device-cluster", "device", "http://localhost:5054");
string fhirUrl = GetBackendAddress("fhir-cluster", "fhir", "http://localhost:5055");
string cdsUrl = GetBackendAddress("cds-cluster", "cds", "http://localhost:5056");
string reportsUrl = GetBackendAddress("reports-cluster", "reports", "http://localhost:5057");

builder.Services.AddHealthChecks()
    .AddNtpSyncCheck()
    .AddUrlGroup(new Uri(patientUrl.TrimEnd('/') + "/health"), "patient-api")
    .AddUrlGroup(new Uri(prescriptionUrl.TrimEnd('/') + "/health"), "prescription-api")
    .AddUrlGroup(new Uri(treatmentUrl.TrimEnd('/') + "/health"), "treatment-api")
    .AddUrlGroup(new Uri(alarmUrl.TrimEnd('/') + "/health"), "alarm-api")
    .AddUrlGroup(new Uri(deviceUrl.TrimEnd('/') + "/health"), "device-api")
    .AddUrlGroup(new Uri(fhirUrl.TrimEnd('/') + "/health"), "fhir-api")
    .AddUrlGroup(new Uri(cdsUrl.TrimEnd('/') + "/health"), "cds-api")
    .AddUrlGroup(new Uri(reportsUrl.TrimEnd('/') + "/health"), "reports-api");

WebApplication app = builder.Build();

if (corsOrigins.Length > 0)
    app.UseCors();

app.MapReverseProxy();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            serverTimeUtc = DateTime.UtcNow.ToString("O"),
            entries = report.Entries.ToDictionary(e => e.Key, e => new
            {
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.MapOpenApi();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

await app.RunAsync();

namespace Dialysis.Gateway { public partial class Program { } }
