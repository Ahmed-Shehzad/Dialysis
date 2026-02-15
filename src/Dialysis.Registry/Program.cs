using System.Reflection;
using Dialysis.Auth;
using Dialysis.Configuration;
using Dialysis.HealthChecks;
using Dialysis.Observability;
using Dialysis.ApiClients;
using Dialysis.Registry.Adapters;
using Dialysis.Registry.Configuration;
using Dialysis.Registry.Middleware;
using Dialysis.Registry.Services;
using Dialysis.Tenancy;
using Intercessor;
using Microsoft.Extensions.Options;
using Refit;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyVaultIfConfigured();

builder.Services.Configure<RegistryOptions>(builder.Configuration.GetSection(RegistryOptions.SectionName));

builder.Services.AddRefitClient<Dialysis.ApiClients.IFhirApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        var opts = sp.GetRequiredService<IOptions<RegistryOptions>>().Value;
        c.BaseAddress = new Uri(opts.FhirBaseUrl.TrimEnd('/') + "/");
    });

builder.Services.AddRefitClient<IFhirBundleApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        var opts = sp.GetRequiredService<IOptions<RegistryOptions>>().Value;
        c.BaseAddress = new Uri(opts.FhirBaseUrl.TrimEnd('/') + "/");
    });
builder.Services.AddScoped<IFhirBundleClient, RefitFhirBundleClient>();

builder.Services.AddScoped<IRegistryAdapter, EsrdAdapter>();
builder.Services.AddScoped<IRegistryAdapter, QipAdapter>();
builder.Services.AddScoped<IRegistryAdapter, CrownWebAdapter>();
builder.Services.AddScoped<IRegistryAdapter, NhsnAdapter>();
builder.Services.AddScoped<IRegistryAdapter, VascularAccessAdapter>();

builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddTenancy(builder.Configuration);

builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-registry");
builder.Services.AddDialysisHealthChecks(builder.Configuration);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();

builder.Services.AddIntercessor(cfg => cfg.RegisterFromAssembly(Assembly.GetExecutingAssembly()));

var app = builder.Build();

app.ValidateProductionConfig();
app.UseTenantResolution();
app.UseJwtAuthentication();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
