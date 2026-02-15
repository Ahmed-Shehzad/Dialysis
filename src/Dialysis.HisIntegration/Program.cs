using System.Reflection;
using Dialysis.ApiClients;
using Dialysis.Auth;
using Dialysis.Configuration;
using Dialysis.Contracts.Messages;
using Dialysis.HealthChecks;
using Dialysis.Messaging;
using Dialysis.Observability;
using Dialysis.HisIntegration;
using Dialysis.HisIntegration.Features.AdtSync;
using Dialysis.HisIntegration.Workers;
using Dialysis.HisIntegration.Middleware;
using Dialysis.HisIntegration.Services;
using Dialysis.Tenancy;
using Intercessor;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyVaultIfConfigured();

var serviceBusConnection = builder.Configuration["Hl7Stream:ConnectionString"]
    ?? builder.Configuration["ServiceBus:ConnectionString"]
    ?? builder.Configuration.GetConnectionString("ServiceBus");
var hisAddress = new Uri("sb://dialysis/his-integration");
builder.Services.AddDialysisTransponder(hisAddress, serviceBusConnection);
builder.Services.AddMessageConsumer<Hl7Ingested>(
    new Uri("sb://dialysis/hl7-ingest/subscriptions/his-subscription"));
builder.Services.AddScoped<IMessageHandler<Hl7Ingested>, Hl7IngestedHandler>();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-his-integration");
builder.Services.AddDialysisHealthChecks(builder.Configuration)
    .AddServiceBusHealthCheck(builder.Configuration);
builder.Services.AddTenancy(builder.Configuration);
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IFhirApiFactory, FhirApiFactory>();
builder.Services.AddScoped<ITenantFhirResolver, TenantFhirResolver>();
builder.Services.AddScoped<IAzureConvertDataClient, AzureConvertDataClient>();
builder.Services.AddControllers();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();
builder.Services.Configure<FhirAdtWriterOptions>(builder.Configuration.GetSection(FhirAdtWriterOptions.SectionName));
builder.Services.AddScoped<IFhirAdtWriter, FhirAdtWriter>();
builder.Services.AddScoped<IProvenanceRecorder, ProvenanceRecorder>();
builder.Services.AddScoped<IHl7StreamingWriter, AzureHl7StreamingWriter>();
builder.Services.Configure<AzureConvertDataOptions>(builder.Configuration.GetSection(AzureConvertDataOptions.SectionName));
builder.Services.AddSingleton<AdtMessageParser>();
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
