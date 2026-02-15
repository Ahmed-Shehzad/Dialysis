using System.Reflection;
using BuildingBlocks.Abstractions;
using Dialysis.Configuration;
using Dialysis.Auth;
using Dialysis.HealthChecks;
using Dialysis.Observability;
using Dialysis.Contracts.Events;
using Dialysis.Messaging;
using FhirCore.Subscriptions;
using FhirCore.Subscriptions.Data;
using FhirCore.Subscriptions.Services;
using Intercessor;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyVaultIfConfigured();

var serviceBusConnection = builder.Configuration["ServiceBus:ConnectionString"]
    ?? builder.Configuration.GetConnectionString("ServiceBus");
var subscriptionsAddress = new Uri("sb://dialysis/subscriptions");
builder.Services.AddDialysisTransponder(subscriptionsAddress, serviceBusConnection);
builder.Services.AddIntegrationEventConsumer<ResourceWrittenEvent>(
    new Uri("sb://dialysis/resource-written/subscriptions/subscriptions-subscription"));
builder.Services.AddScoped<IIntegrationEventHandler<ResourceWrittenEvent>, ResourceWrittenIntegrationEventHandler>();

builder.Services.AddHttpClient();
var conn = builder.Configuration.GetConnectionString("Subscriptions") ?? builder.Configuration.GetConnectionString("DefaultConnection") ?? "Host=localhost;Database=fhir_subscriptions;Username=postgres;Password=postgres";
builder.Services.AddDbContextFactory<SubscriptionDbContext>(o => o.UseNpgsql(conn));
builder.Services.AddSingleton<ISubscriptionsStore, EfSubscriptionsStore>();
builder.Services.AddSingleton<ICriteriaMatcher, CriteriaMatcher>();
builder.Services.AddSingleton<IWebhookNotifier, WebhookNotifier>();
builder.Services.AddSingleton<ISubscriptionNotificationService, SubscriptionNotificationService>();
builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-fhir-subscriptions");
builder.Services.AddDialysisHealthChecks(builder.Configuration)
    .AddServiceBusHealthCheck(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();
builder.Services.AddIntercessor(cfg => cfg.RegisterFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddControllers();

var app = builder.Build();

app.ValidateProductionConfig();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SubscriptionDbContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

app.UseJwtAuthentication();
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();
