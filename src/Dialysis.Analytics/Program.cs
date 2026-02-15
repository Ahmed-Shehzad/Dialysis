using System.Reflection;
using Dialysis.Analytics.Configuration;
using Dialysis.Analytics.Middleware;
using Dialysis.Analytics.Data;
using Dialysis.Analytics.Services;
using Dialysis.ApiClients;
using Dialysis.Auth;
using Dialysis.Configuration;
using Dialysis.HealthChecks;
using Dialysis.Observability;
using Dialysis.Tenancy;
using Intercessor;
using Microsoft.Extensions.Options;
using Refit;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyVaultIfConfigured();

builder.Services.Configure<AnalyticsOptions>(builder.Configuration.GetSection(AnalyticsOptions.SectionName));
builder.Services.AddHttpContextAccessor();

builder.Services.AddRefitClient<IFhirApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        var opts = sp.GetRequiredService<IOptions<AnalyticsOptions>>().Value;
        c.BaseAddress = new Uri(opts.FhirBaseUrl.TrimEnd('/') + "/");
    })
    .AddHttpMessageHandler<ForwardingHttpHandler>();

builder.Services.AddRefitClient<IAlertingApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        var opts = sp.GetRequiredService<IOptions<AnalyticsOptions>>().Value;
        c.BaseAddress = new Uri(opts.AlertingBaseUrl.TrimEnd('/') + "/");
    })
    .AddHttpMessageHandler<ForwardingHttpHandler>();

builder.Services.AddRefitClient<IFhirBundleApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        var opts = sp.GetRequiredService<IOptions<AnalyticsOptions>>().Value;
        c.BaseAddress = new Uri(opts.FhirBaseUrl.TrimEnd('/') + "/");
    })
    .AddHttpMessageHandler<ForwardingHttpHandler>();
builder.Services.AddScoped<IFhirBundleClient, RefitFhirBundleClient>();

var auditBaseUrl = builder.Configuration.GetSection(AnalyticsOptions.SectionName)["AuditConsentBaseUrl"];
if (!string.IsNullOrWhiteSpace(auditBaseUrl))
{
    builder.Services.AddRefitClient<IAuditConsentApi>()
        .ConfigureHttpClient((sp, c) => c.BaseAddress = new Uri(auditBaseUrl.TrimEnd('/') + "/"))
        .AddHttpMessageHandler<ForwardingHttpHandler>();
    builder.Services.AddScoped<IAnalyticsAuditRecorder, RefitAnalyticsAuditRecorder>();
    builder.Services.AddScoped<IConsentVerificationClient, RefitConsentVerificationClient>();
}
else
{
    builder.Services.AddSingleton<IAnalyticsAuditRecorder, NoOpAnalyticsAuditRecorder>();
    builder.Services.AddSingleton<IConsentVerificationClient, NoOpConsentVerificationClient>();
}

var publicHealthBaseUrl = builder.Configuration.GetSection(AnalyticsOptions.SectionName)["PublicHealthBaseUrl"];
if (!string.IsNullOrWhiteSpace(publicHealthBaseUrl))
{
    builder.Services.AddRefitClient<IPublicHealthDeidentifyApi>()
        .ConfigureHttpClient((sp, c) => c.BaseAddress = new Uri(publicHealthBaseUrl.TrimEnd('/') + "/"))
        .AddHttpMessageHandler<ForwardingHttpHandler>();
    builder.Services.AddScoped<IDeidentificationApiClient, RefitDeidentificationApiClient>();
}
else
{
    builder.Services.AddSingleton<IDeidentificationApiClient, NoOpDeidentificationApiClient>();
}

builder.Services.AddTransient<ForwardingHttpHandler>();

var analyticsConn = builder.Configuration.GetConnectionString("Analytics")
    ?? builder.Configuration["ConnectionStrings:Analytics"];
if (!string.IsNullOrWhiteSpace(analyticsConn))
{
    builder.Services.Configure<AnalyticsOptions>(o => o.ConnectionString = analyticsConn);
    builder.Services.AddScoped<ISavedCohortStore, PostgresSavedCohortStore>();
}
else
{
    builder.Services.AddSingleton<ISavedCohortStore, InMemorySavedCohortStore>();
}

builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddTenancy(builder.Configuration);

builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-analytics");
builder.Services.AddDialysisHealthChecks(builder.Configuration);

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();

builder.Services.AddIntercessor(cfg => cfg.RegisterFromAssembly(Assembly.GetExecutingAssembly()));

var app = builder.Build();

if (!string.IsNullOrWhiteSpace(analyticsConn))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISavedCohortStore>();
        if (store is PostgresSavedCohortStore pgStore)
            await pgStore.EnsureTableAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Failed to ensure saved_cohorts table");
    }
}

app.ValidateProductionConfig();
app.UseTenantResolution();
app.UseJwtAuthentication();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
