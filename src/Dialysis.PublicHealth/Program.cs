using System.Reflection;
using Dialysis.ApiClients;
using Dialysis.Auth;
using Dialysis.Configuration;
using Dialysis.Contracts.Messages;
using Dialysis.HealthChecks;
using Dialysis.Messaging;
using Dialysis.Observability;
using Dialysis.PublicHealth.Configuration;
using Dialysis.PublicHealth.Features.Reports.Sagas;
using Dialysis.PublicHealth.Middleware;
using Dialysis.PublicHealth.Services;
using Dialysis.Tenancy;
using Intercessor;
using Microsoft.Extensions.Options;
using Refit;
using Transponder.Abstractions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyVaultIfConfigured();

var serviceBusConnection = builder.Configuration["ServiceBus:ConnectionString"]
    ?? builder.Configuration.GetConnectionString("ServiceBus");
var publicHealthAddress = new Uri("sb://dialysis/public-health");
builder.Services.AddDialysisTransponder(publicHealthAddress, serviceBusConnection, options =>
{
    options.UseSagaOrchestration(cfg => cfg.AddSaga<ReportDeliverySaga, ReportDeliveryState>(b =>
        b.StartWith<DeliverReportSagaMessage>(new Uri("sb://dialysis/report-delivery-saga/subscriptions/saga"))));
});

builder.Services.Configure<PublicHealthOptions>(builder.Configuration.GetSection(PublicHealthOptions.SectionName));
builder.Services.AddHttpContextAccessor();

builder.Services.AddRefitClient<Dialysis.ApiClients.IFhirApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        var opts = sp.GetRequiredService<IOptions<PublicHealthOptions>>().Value;
        c.BaseAddress = new Uri(opts.FhirBaseUrl.TrimEnd('/') + "/");
    });

builder.Services.AddScoped<IReportableConditionCatalog>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<PublicHealthOptions>>().Value;
    return new ConfigurableReportableConditionCatalog(opts.ReportableConditionsConfigPath);
});
builder.Services.AddScoped<IReportGenerator, FhirMeasureReportGenerator>();
builder.Services.AddScoped<IMeasureReportToHl7V2Converter, MeasureReportToHl7V2Converter>();
builder.Services.AddScoped<IDeidentificationPipeline, DeidentificationPipeline>();
builder.Services.AddScoped<ReportableConditionMatcher>();

var reportDeliveryEndpoint = builder.Configuration.GetSection(PublicHealthOptions.SectionName)["ReportDeliveryEndpoint"];
if (!string.IsNullOrWhiteSpace(reportDeliveryEndpoint))
{
    builder.Services.AddRefitClient<IReportDeliveryApi>()
        .ConfigureHttpClient((sp, c) => c.BaseAddress = new Uri(reportDeliveryEndpoint.TrimEnd('/')));
    builder.Services.AddScoped<IReportDeliveryService, RefitReportDeliveryService>();
}
else
{
    builder.Services.AddSingleton<IReportDeliveryService, NoOpReportDeliveryService>();
}

builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddTenancy(builder.Configuration);

builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-public-health");
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
