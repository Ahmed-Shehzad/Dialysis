using System.Reflection;
using Dialysis.ApiClients;
using Dialysis.Auth;
using Dialysis.Configuration;
using Dialysis.HealthChecks;
using Dialysis.Observability;
using Dialysis.DeviceIngestion;
using Dialysis.DeviceIngestion.Http;
using Dialysis.DeviceIngestion.Middleware;
using Dialysis.DeviceIngestion.Services;
using Dialysis.Tenancy;
using Intercessor;
using Microsoft.Extensions.Options;
using Refit;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyVaultIfConfigured();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-device-ingestion");
builder.Services.AddDialysisHealthChecks(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddTenancy(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<FhirObservationWriterOptions>(builder.Configuration.GetSection(FhirObservationWriterOptions.SectionName));
builder.Services.AddRefitClient<IFhirApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        var opts = sp.GetRequiredService<IOptions<FhirObservationWriterOptions>>().Value;
        c.BaseAddress = new Uri((opts.BaseUrl ?? "https://localhost:5000/fhir").TrimEnd('/') + "/");
    })
    .AddHttpMessageHandler<TenantIdForwardingHandler>();
builder.Services.AddTransient<TenantIdForwardingHandler>();
builder.Services.AddScoped<IFhirObservationWriter, FhirObservationWriter>();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();
builder.Services.AddIntercessor(cfg => cfg.RegisterFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

var app = builder.Build();

app.ValidateProductionConfig();
app.UseTenantResolution();
app.UseExceptionHandler(_ => { });
app.UseJwtAuthentication();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
