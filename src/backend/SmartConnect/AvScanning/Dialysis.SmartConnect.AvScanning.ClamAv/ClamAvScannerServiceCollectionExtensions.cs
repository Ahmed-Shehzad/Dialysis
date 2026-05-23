using Dialysis.SmartConnect.Attachments;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.AvScanning.ClamAv;

/// <summary>
/// Composition helpers for the ClamAV scanner adapter.
/// </summary>
public static class ClamAvScannerServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ClamAvAttachmentBlobScanner"/> as the active <see cref="IAttachmentBlobScanner"/>.
    /// Singleton — TCP connections to clamd are short-lived per scan, so there's no client to share.
    /// </summary>
    public static IServiceCollection UseClamAvAttachmentBlobScanner(
        this IServiceCollection services,
        Action<ClamAvScannerOptions>? configure = null)
    {
        if (configure is not null)
        {
            services.Configure(configure);
        }
        else
        {
            services.AddOptions<ClamAvScannerOptions>();
        }
        services.RemoveAll<IAttachmentBlobScanner>();
        services.AddSingleton<IAttachmentBlobScanner, ClamAvAttachmentBlobScanner>();
        return services;
    }
}
