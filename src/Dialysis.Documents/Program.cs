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
using Intercessor;
using Microsoft.Extensions.Options;
using Refit;

var builder = WebApplication.CreateBuilder(args);
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

builder.Services.AddScoped<IPdfGenerator, QuestPdfGenerator>();
builder.Services.AddScoped<IPdfTemplateFiller, TextPdfTemplateFiller>();
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
