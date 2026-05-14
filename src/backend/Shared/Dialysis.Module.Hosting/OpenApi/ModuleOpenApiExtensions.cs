using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.Module.Hosting.OpenApi;

public static class ModuleOpenApiExtensions
{
    /// <summary>
    /// Registers URL-segment-based API versioning (e.g. <c>/api/v1.0/...</c>) and a single OpenAPI document.
    /// Modules that need multiple versioned documents can extend this in their own composition root.
    /// </summary>
    public static IServiceCollection AddModuleApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.AddOpenApi();
        return services;
    }
}
