using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.Dicom.Dimse;

/// <summary>
/// Composition helper for the DIMSE C-STORE SCP hosted service.
/// </summary>
public static class DimseCStoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers the DIMSE C-STORE SCP. The host must also register <c>AddDicomIngestion()</c> so
    /// the SCP can resolve the ingestion pipeline per-association.
    /// </summary>
    public static IServiceCollection AddDimseCStoreScp(
        this IServiceCollection services,
        Action<DimseCStoreOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<DimseCStoreOptions>();
        }
        services.AddHostedService<DimseCStoreHostedService>();
        return services;
    }
}
