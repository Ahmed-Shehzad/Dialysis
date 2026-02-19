using BuildingBlocks.Authorization;
using BuildingBlocks.Tenancy;

using Dialysis.Fhir.Api;
using Dialysis.Fhir.Api.Subscriptions;

using Microsoft.AspNetCore.Authentication.JwtBearer;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddControllers();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
builder.Services.AddOpenApi();
builder.Services.AddTenantResolution();

string baseUrl = builder.Configuration["FhirExport:BaseUrl"] ?? "http://localhost:5000";
builder.Services.AddHttpClient<FhirBulkExportService>(client =>
{
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/fhir+json");
});
builder.Services.AddScoped<FhirBulkExportService>();

builder.Services.AddSingleton<ISubscriptionStore, InMemorySubscriptionStore>();
builder.Services.AddHttpClient<SubscriptionDispatcher>(client =>
{
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/fhir+json");
});
builder.Services.AddScoped<SubscriptionDispatcher>();

WebApplication app = builder.Build();

app.UseTenantResolution();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();
