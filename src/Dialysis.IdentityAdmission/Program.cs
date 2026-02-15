using System.Reflection;
using Dialysis.ApiClients;
using Dialysis.Auth;
using Dialysis.Configuration;
using Dialysis.HealthChecks;
using Dialysis.Observability;
using Dialysis.IdentityAdmission;
using Dialysis.IdentityAdmission.Services;
using Intercessor;
using Microsoft.Extensions.Options;
using Refit;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddKeyVaultIfConfigured();

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddDialysisOpenTelemetry(builder.Configuration, "dialysis-identity-admission");
builder.Services.AddDialysisHealthChecks(builder.Configuration);
builder.Services.AddControllers();
builder.Services.Configure<FhirIdentityWriterOptions>(builder.Configuration.GetSection(FhirIdentityWriterOptions.SectionName));
builder.Services.AddRefitClient<IFhirApi>()
    .ConfigureHttpClient((sp, c) =>
    {
        var opts = sp.GetRequiredService<IOptions<FhirIdentityWriterOptions>>().Value;
        c.BaseAddress = new Uri((opts.BaseUrl ?? "https://localhost:5000/fhir").TrimEnd('/') + "/");
    });
builder.Services.AddScoped<IFhirIdentityWriter, FhirIdentityWriter>();
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc();
builder.Services.AddIntercessor(cfg => cfg.RegisterFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

var app = builder.Build();

app.ValidateProductionConfig();
app.UseExceptionHandler(_ => { });
app.UseJwtAuthentication();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
