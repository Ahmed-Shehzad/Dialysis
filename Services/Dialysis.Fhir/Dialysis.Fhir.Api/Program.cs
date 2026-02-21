using System.Text.Json;

using BuildingBlocks.Authorization;
using BuildingBlocks.ExceptionHandling;
using BuildingBlocks.Logging;
using BuildingBlocks.Options;
using BuildingBlocks.Tenancy;

using Dialysis.Fhir.Abstractions;

using Serilog;
using Refit;
using Dialysis.Fhir.Api;
using Dialysis.Fhir.Api.Subscriptions;
using Dialysis.Fhir.Infrastructure.Persistence;

using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, config) =>
    config.ReadFrom.Configuration(context.Configuration)
        .Enrich.With<ActivityEnricher>()
        .Enrich.FromLogContext());

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
#pragma warning disable CS0618
        JsonSerializerOptions fhirOpts = new JsonSerializerOptions().ForFhir(typeof(ModelInfo).Assembly);
#pragma warning restore CS0618
        foreach (System.Text.Json.Serialization.JsonConverter converter in fhirOpts.Converters)
            o.JsonSerializerOptions.Converters.Add(converter);
    });

builder.Services.AddJwtBearerStartupValidation(builder.Configuration);
builder.Services.AddCentralExceptionHandler(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.Authority = builder.Configuration["Authentication:JwtBearer:Authority"];
        opts.Audience = builder.Configuration["Authentication:JwtBearer:Audience"] ?? "api://dialysis-pdms";
        opts.RequireHttpsMetadata = builder.Configuration.GetValue("Authentication:JwtBearer:RequireHttpsMetadata", true);
    });
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ScopeOrBypassHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("FhirExport", p => p.Requirements.Add(new ScopeOrBypassRequirement("Patient:Read", "Prescription:Read", "Treatment:Read", "Alarm:Read", "Device:Read")));
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
builder.Services.AddOpenApi();
builder.Services.AddTenantResolution();

string baseUrl = builder.Configuration["FhirExport:BaseUrl"] ?? "http://localhost:5000";
builder.Services.AddRefitClient<IFhirExportGatewayApi>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/fhir+json");
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .AddStandardResilienceHandler();
builder.Services.AddScoped<FhirBulkExportService>();

string? fhirConnectionString = builder.Configuration.GetConnectionString("FhirDb");
if (!string.IsNullOrEmpty(fhirConnectionString))
{
    builder.Services.AddDbContext<FhirDbContext>(o => o.UseNpgsql(fhirConnectionString));
    builder.Services.AddScoped<ISubscriptionStore, PostgresSubscriptionStore>();
    builder.Services.AddHealthChecks().AddNpgSql(fhirConnectionString, name: "fhir-db");
}
else builder.Services.AddSingleton<ISubscriptionStore, InMemorySubscriptionStore>();

builder.Services.AddHttpClient<SubscriptionDispatcher>(client =>
{
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/fhir+json");
});
builder.Services.AddScoped<SubscriptionDispatcher>();

WebApplication app = builder.Build();

app.UseTenantResolution();
app.UseCentralExceptionHandler();
if (app.Environment.IsDevelopment() && !string.IsNullOrEmpty(fhirConnectionString))
{
    using IServiceScope scope = app.Services.CreateScope();
    FhirDbContext db = scope.ServiceProvider.GetRequiredService<FhirDbContext>();
    await db.Database.MigrateAsync();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
