using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NutrientPDF.Abstractions;
using NutrientPDF.Decorators;

namespace NutrientPDF;

/// <summary>
/// Extension methods to register NutrientPDF services.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Registers NutrientPDF document processing services with all decorators.
    /// Decorator chain: <see cref="ValidatingNutrientPdfService"/> → <see cref="LoggingNutrientPdfService"/> → <see cref="NutrientPdfService"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configure">Optional callback to configure NutrientPDF options (e.g. license key).</param>
    /// <returns>The same service collection instance.</returns>
    public static IServiceCollection AddNutrientPdf(this IServiceCollection services, Action<NutrientPdfOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);

        services.AddSingleton<NutrientPdfService>();

        services.AddSingleton<INutrientPdfService>(sp =>
        {
            var core = sp.GetRequiredService<NutrientPdfService>();
            var logger = sp.GetService<ILogger<LoggingNutrientPdfService>>()
                ?? NullLogger<LoggingNutrientPdfService>.Instance;

            INutrientPdfService inner = new LoggingNutrientPdfService(core, logger);
            return new ValidatingNutrientPdfService(inner);
        });

        return services;
    }
}
