using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Dicom.Persistence;

/// <summary>
/// Composition helper for the DICOM persistence + ingestion services. Adds the EF-backed instance
/// store and the default ingestion pipeline. Hosts must register an <c>IAttachmentBlobStore</c>
/// before calling this so the ingestion service can persist DICOM bytes.
/// </summary>
public static class DicomServiceCollectionExtensions
{
    public static IServiceCollection AddDicomIngestion(this IServiceCollection services)
    {
        services.TryAddScoped<IDicomInstanceStore, EfDicomInstanceStore>();
        services.TryAddScoped<IDicomIngestionService, DicomIngestionService>();
        // No-op by default — a host opts into order correlation by registering the real notifier
        // (Dicom.Integration bridge) before this call, since TryAdd won't overwrite it.
        services.TryAddScoped<IImagingStudyLinkNotifier, NoopImagingStudyLinkNotifier>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
