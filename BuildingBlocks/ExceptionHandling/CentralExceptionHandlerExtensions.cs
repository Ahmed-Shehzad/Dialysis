using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.ExceptionHandling;

/// <summary>
/// Central exception handling for APIs. Returns RFC 7807 Problem Details.
/// Stack trace only in Development; in Production, emails full report to dev inbox.
/// </summary>
public static class CentralExceptionHandlerExtensions
{
    /// <summary>
    /// Registers the central exception handler and exception report email sender.
    /// When configuration is provided, binds ExceptionHandling:Email. The sender no-ops when disabled.
    /// Call before Build().
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Optional. When provided, configures exception report email. Pass builder.Configuration to enable.</param>
    public static IServiceCollection AddCentralExceptionHandler(this IServiceCollection services, IConfiguration? configuration = null)
    {
        services.AddExceptionHandler<CentralExceptionHandler>();

        if (configuration is not null)
            services.Configure<ExceptionReportEmailOptions>(configuration.GetSection(ExceptionReportEmailOptions.SectionName));

        return services.AddSingleton<IExceptionReportEmailSender, ExceptionReportEmailSender>();
    }

    /// <summary>
    /// Uses the central exception handler pipeline. Returns application/problem+json for all exceptions.
    /// Requires AddCentralExceptionHandler() to be called.
    /// </summary>
    public static IApplicationBuilder UseCentralExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(_ => { });
    }
}
