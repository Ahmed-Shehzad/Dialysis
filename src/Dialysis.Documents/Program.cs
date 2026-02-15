using System.Reflection;
using Dialysis.Auth;
using Dialysis.Configuration;
using Dialysis.ApiClients;
using Dialysis.Documents.Configuration;
using Dialysis.Documents.Middleware;
using Dialysis.Documents.Services;
using Dialysis.HealthChecks;
using Dialysis.Observability;
using Dialysis.Tenancy;
using GdPicture14;
using Intercessor;
using Microsoft.Extensions.Options;
using Refit;

var builder = WebApplication.CreateBuilder(args);

// Nutrient (GdPicture) license: empty string = trial mode. Set NutrientLicenseKey in configuration for production.
var nutrientKey = builder.Configuration.GetSection(DocumentsOptions.SectionName)["NutrientLicenseKey"] ?? "";
new LicenseManager().RegisterKEY(nutrientKey);
builder.Configuration.AddKeyVaultIfConfigured();

builder.Services.Configure<DocumentsOptions>(builder.Configuration.GetSection(DocumentsOptions.SectionName));
builder.Services.AddHttpContextAccessor();

builder.Services.AddRefitClient<Dialysis.ApiClients.IFhirApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        var opts = sp.GetRequiredService<IOptions<DocumentsOptions>>().Value;
        c.BaseAddress = new Uri(opts.FhirBaseUrl.TrimEnd('/') + "/");
    });

builder.Services.AddRefitClient<IFhirBinaryApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        var opts = sp.GetRequiredService<IOptions<DocumentsOptions>>().Value;
        c.BaseAddress = new Uri(opts.FhirBaseUrl.TrimEnd('/') + "/");
    });
builder.Services.AddScoped<IFhirBinaryClient, RefitFhirBinaryClient>();

builder.Services.AddScoped<IPdfGenerator, NutrientPdfGenerator>();
builder.Services.AddScoped<IPdfTemplateFiller, NutrientPdfTemplateFiller>();
builder.Services.AddScoped<IPdfBarcodeService, NutrientPdfBarcodeService>();
builder.Services.AddScoped<IPdfSignatureService, NutrientPdfSignatureService>();
builder.Services.AddScoped<IFhirDataResolver, FhirDataResolver>();
builder.Services.AddScoped<IBundleToPdfConverter, BundleToPdfConverter>();
builder.Services.AddScoped<IDocumentStore, FhirDocumentStore>();

builder.Services.AddControllers();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddTenancy(builder.Configuration);

builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-documents");
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
