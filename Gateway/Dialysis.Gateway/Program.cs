using Microsoft.AspNetCore.Diagnostics.HealthChecks;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddOpenApi();

string GetBackendAddress(string cluster, string destination, string @default) =>
    builder.Configuration[$"ReverseProxy:Clusters:{cluster}:Destinations:{destination}:Address"] ?? @default;

string patientUrl = GetBackendAddress("patient-cluster", "patient", "http://localhost:5051");
string prescriptionUrl = GetBackendAddress("prescription-cluster", "prescription", "http://localhost:5052");
string treatmentUrl = GetBackendAddress("treatment-cluster", "treatment", "http://localhost:5050");
string alarmUrl = GetBackendAddress("alarm-cluster", "alarm", "http://localhost:5053");
string deviceUrl = GetBackendAddress("device-cluster", "device", "http://localhost:5054");

builder.Services.AddHealthChecks()
    .AddUrlGroup(new Uri(patientUrl.TrimEnd('/') + "/health"), "patient-api")
    .AddUrlGroup(new Uri(prescriptionUrl.TrimEnd('/') + "/health"), "prescription-api")
    .AddUrlGroup(new Uri(treatmentUrl.TrimEnd('/') + "/health"), "treatment-api")
    .AddUrlGroup(new Uri(alarmUrl.TrimEnd('/') + "/health"), "alarm-api")
    .AddUrlGroup(new Uri(deviceUrl.TrimEnd('/') + "/health"), "device-api");

WebApplication app = builder.Build();

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

await app.RunAsync();
