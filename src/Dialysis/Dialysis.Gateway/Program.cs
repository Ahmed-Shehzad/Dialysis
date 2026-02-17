using System.Reflection;

using Dialysis.Gateway.Infrastructure;
using Microsoft.Extensions.Options;
using Transponder;
using Transponder.Transports.AzureServiceBus;
using Dialysis.Gateway.Services;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.Gateway.Middleware;
using Dialysis.Persistence;
using Dialysis.Persistence.Abstractions;

using Intercessor;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IPatientIdentifierResolver, LocalPatientIdentifierResolver>();

builder.Services.Configure<EhrOutboundOptions>(
    builder.Configuration.GetSection(EhrOutboundOptions.Section));

var ehrOpts = builder.Configuration.GetSection("Integration").Get<EhrOutboundOptions>();
if (!string.IsNullOrWhiteSpace(ehrOpts?.ClientId) && !string.IsNullOrWhiteSpace(ehrOpts?.ClientSecret))
{
    builder.Services.AddHttpClient<ISmartFhirTokenProvider, SmartEhrTokenProvider>();
}
else
{
    builder.Services.AddSingleton<ISmartFhirTokenProvider, NoOpSmartFhirTokenProvider>();
}
builder.Services.AddHttpClient<IEhrOutboundClient, HttpEhrOutboundClient>();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddControllers();

// C5: Externalize secrets (config, Key Vault, env). No hardcoded production credentials.
var connectionString = builder.Configuration.GetConnectionString("PostgreSQL")
    ?? (builder.Environment.IsDevelopment() ? "Host=localhost;Database=dialysis_gateway;Username=postgres;Password=postgres" : null);
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("ConnectionStrings:PostgreSQL must be set. Use Azure Key Vault or environment variables in production.");

builder.Services.AddDbContext<DialysisDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IObservationRepository, ObservationRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<IVascularAccessRepository, VascularAccessRepository>();
builder.Services.AddScoped<IConditionRepository, ConditionRepository>();
builder.Services.AddScoped<IEpisodeOfCareRepository, EpisodeOfCareRepository>();
builder.Services.AddScoped<IProcessedHl7MessageStore, ProcessedHl7MessageStore>();
builder.Services.AddScoped<IFailedHl7MessageStore, FailedHl7MessageStore>();
builder.Services.AddScoped<IIdMappingRepository, IdMappingRepository>();
builder.Services.Configure<TerminologyOptions>(builder.Configuration.GetSection(TerminologyOptions.Section));
var termOpts = builder.Configuration.GetSection(TerminologyOptions.Section).Get<TerminologyOptions>();
if (termOpts?.IsConfigured == true)
{
    builder.Services.AddHttpClient<ITerminologyService, RefitTerminologyService>(client =>
        client.BaseAddress = new Uri(termOpts.ServerUrl!.TrimEnd('/')));
}
else
{
    builder.Services.AddScoped<ITerminologyService, NoOpTerminologyService>();
}
builder.Services.AddSingleton<Dialysis.SharedKernel.Abstractions.IDeviceMessageAdapter, PassThroughDeviceAdapter>();

builder.Services.AddScoped<IPatientDataService, PatientDataService>();
builder.Services.AddScoped<IFhirBundleBuilder, FhirBundleBuilder>();
builder.Services.AddScoped<IQualityBundleService, QualityBundleService>();
builder.Services.AddScoped<IDeidentificationService, NoOpDeidentificationService>();
builder.Services.AddScoped<Dialysis.Gateway.Features.SessionSummary.SessionSummaryPublisher>();

builder.Services.Configure<EventExportOptions>(builder.Configuration.GetSection(EventExportOptions.Section));
var eventExportOpts = builder.Configuration.GetSection(EventExportOptions.Section).Get<EventExportOptions>();
if (eventExportOpts?.IsConfigured == true)
{
    var topic = eventExportOpts.Topic.Trim();
    var address = new Uri($"sb://dialysis/{topic}");
    builder.Services.AddTransponder(address, opt =>
    {
        opt.TransportBuilder.UseAzureServiceBus(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<EventExportOptions>>().Value;
            var topicName = opts.Topic.Trim();
            var hostAddress = new Uri($"sb://dialysis/{topicName}");
            var topology = new MappingAzureServiceBusTopology(
                new Dictionary<Type, string> { { typeof(EventExportMessage), topicName } });
            return new AzureServiceBusHostSettings(hostAddress, topology, connectionString: opts.ConnectionString);
        });
    });
    builder.Services.AddHostedService<TransponderBusHostedService>();
    builder.Services.AddSingleton<IEventExportPublisher, AzureServiceBusEventExportPublisher>();
}
else
{
    builder.Services.AddSingleton<IEventExportPublisher, NoOpEventExportPublisher>();
}

builder.Services.AddIntercessor(cfg =>
{
    cfg.RegisterFromAssembly(Assembly.GetExecutingAssembly());
    cfg.RegisterFromAssembly(typeof(Dialysis.Alerting.HypotensionRiskPredictionHandler).Assembly);
    cfg.RegisterFromAssembly(typeof(Dialysis.DeviceIngestion.Features.Vitals.Ingest.IngestVitalsHandler).Assembly);
});

builder.Services.AddExceptionHandler<Dialysis.Gateway.ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<Dialysis.Gateway.PatientConflictExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.Configure<SmartServerOptions>(builder.Configuration.GetSection(SmartServerOptions.Section));
builder.Services.AddSingleton<IAuthorizationCodeStore, InMemoryAuthorizationCodeStore>();
builder.Services.AddSingleton<ISmartJwtIssuer, SmartJwtIssuer>();

var smartOpts = builder.Configuration.GetSection(SmartServerOptions.Section).Get<SmartServerOptions>();
if (smartOpts?.IsConfigured == true)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidIssuer = smartOpts.BaseUrl,
                ValidAudience = smartOpts.BaseUrl,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(smartOpts.SigningKey!)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });
}

// C5: When JWT is configured, require auth for all endpoints except [AllowAnonymous]
builder.Services.AddAuthorization(options =>
{
    if (smartOpts?.IsConfigured == true)
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .Build();
    }
});

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

var app = builder.Build();

// C5: In Production, JWT must be configured; otherwise all business APIs would be unauthenticated
if (app.Environment.IsProduction() && smartOpts?.IsConfigured != true)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("C5: Smart (JWT) is not configured. All APIs are unauthenticated. Set Smart:BaseUrl, SigningKey, ClientId, ClientSecret for production.");
}

if (smartOpts?.IsConfigured == true)
    app.UseAuthentication();
app.UseAuthorization();
app.UseTenantResolution();
app.UseExceptionHandler();
app.MapControllers();
app.MapHealthChecks("/health/ready").AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DialysisDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not run migrations. Ensure PostgreSQL is available.");
    }
}

app.Run();
