using Dialysis.Auth;
using Dialysis.Configuration;
using Dialysis.HealthChecks;
using Dialysis.Messaging;
using Dialysis.Observability;
using FhirCore.Gateway.Configuration;
using FhirCore.Gateway.Validation;
using FhirCore.Packages;
using Hl7.Fhir.Specification.Source;
using Hl7.Fhir.Specification.Terminology;
using Transponder.Abstractions;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyVaultIfConfigured();

var connectionString = builder.Configuration.GetSection(ServiceBusOptions.SectionName)["ConnectionString"];
var gatewayAddress = new Uri("sb://dialysis/fhir-gateway");
builder.Services.AddDialysisTransponder(gatewayAddress, connectionString);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var igLoader = new IgLoader();
    var igPath = config["Fhir:IgProfilesPath"] ?? "ig-profiles";
    var fullPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", igPath);
    igLoader.LoadFromDirectory(Path.GetFullPath(fullPath));
    return igLoader;
});
builder.Services.AddSingleton<IIgLoader>(sp => sp.GetRequiredService<IgLoader>());
builder.Services.AddSingleton<IgResourceResolver>();
builder.Services.AddSingleton<IAsyncResourceResolver>(sp =>
{
    var core = SourceFactory.CreateCachedOffline();
    var ig = sp.GetRequiredService<IgResourceResolver>();
    return new MultiResolver(core, ig);
});
builder.Services.AddSingleton<ICodeValidationTerminologyService>(sp =>
{
    var resolver = sp.GetRequiredService<IAsyncResourceResolver>();
    return LocalTerminologyService.CreateDefaultForCore(resolver);
});
builder.Services.AddSingleton<FhirValidatorService>();
builder.Services.AddSingleton<ProfileResolverClient>();
builder.Services.AddSingleton<OperationOutcomeWriter>();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-gateway");
builder.Services.AddHealthChecks()
    .AddServiceBusHealthCheck(builder.Configuration);

var app = builder.Build();

app.ValidateProductionConfig();
app.UseMiddleware<FhirValidationMiddleware>();
app.UseJwtAuthentication();
app.MapHealthChecks("/health");
app.MapReverseProxy().RequireAuthorization();

app.Run();
