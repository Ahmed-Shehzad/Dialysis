using System.Reflection;
using Dialysis.ApiClients;
using Refit;
using Dialysis.Auth;
using Dialysis.Configuration;
using Dialysis.EHealthGateway.Configuration;
using Dialysis.EHealthGateway.Middleware;
using Dialysis.EHealthGateway.Services;
using Dialysis.HealthChecks;
using Dialysis.Observability;
using Dialysis.Tenancy;
using Intercessor;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyVaultIfConfigured();

builder.Services.Configure<EHealthOptions>(builder.Configuration.GetSection(EHealthOptions.SectionName));
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddTransient<ForwardingHttpHandler>();
builder.Services.AddScoped<IDocumentContentResolver, DocumentContentResolver>();

var auditConsentBaseUrl = builder.Configuration.GetSection(EHealthOptions.SectionName)["AuditConsentBaseUrl"];
if (!string.IsNullOrWhiteSpace(auditConsentBaseUrl))
{
    builder.Services.AddRefitClient<IAuditConsentApi>()
        .ConfigureHttpClient((sp, c) => c.BaseAddress = new Uri(auditConsentBaseUrl.TrimEnd('/') + "/"))
        .AddHttpMessageHandler<ForwardingHttpHandler>();
    builder.Services.AddScoped<IConsentVerificationClient, RefitConsentVerificationClient>();
}
else
{
    builder.Services.AddSingleton<IConsentVerificationClient, NoOpConsentVerificationClient>();
}

// Register eHealth adapter. When certified: replace StubEHealthAdapter with jurisdiction-specific
// adapter (e.g. GematikEpaAdapter when opts.De?.KonnektorUrl is set). See docs/ehealth/CERTIFICATION-CHECKLIST.md.
builder.Services.AddSingleton<IEHealthPlatformAdapter>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<EHealthOptions>>().Value;
    var platform = opts.Platform?.Trim().ToLowerInvariant() ?? "epa";
    return new StubEHealthAdapter(platform);
});

builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddTenancy(builder.Configuration);

builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-ehealth-gateway");
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
