using System.Reflection;
using BuildingBlocks.Abstractions;
using Dialysis.Configuration;
using Dialysis.Contracts.Events;
using Dialysis.HealthChecks;
using Dialysis.Messaging;
using Dialysis.Observability;
using Dialysis.Prediction;
using Dialysis.Prediction.Services;
using Intercessor;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyVaultIfConfigured();

var serviceBusConnection = builder.Configuration["ServiceBus:ConnectionString"]
    ?? builder.Configuration.GetConnectionString("ServiceBus");
var predictionAddress = new Uri("sb://dialysis/prediction");
builder.Services.AddDialysisTransponder(predictionAddress, serviceBusConnection);
builder.Services.AddIntegrationEventConsumer<ObservationCreated>(
    new Uri("sb://dialysis/observation-created/subscriptions/prediction-subscription"));

builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-prediction");
builder.Services.AddHealthChecks()
    .AddServiceBusHealthCheck(builder.Configuration);

builder.Services.Configure<RiskScorerOptions>(builder.Configuration.GetSection(RiskScorerOptions.SectionName));
builder.Services.AddSingleton<IVitalHistoryCache, VitalHistoryCache>();
builder.Services.AddSingleton<IRiskScorer, EnhancedRiskScorer>();
builder.Services.AddScoped<IIntegrationEventHandler<ObservationCreated>, ObservationCreatedIntegrationEventHandler>();
builder.Services.AddIntercessor(cfg => cfg.RegisterFromAssembly(Assembly.GetExecutingAssembly()));

var app = builder.Build();

app.MapHealthChecks("/health");
app.Run();
