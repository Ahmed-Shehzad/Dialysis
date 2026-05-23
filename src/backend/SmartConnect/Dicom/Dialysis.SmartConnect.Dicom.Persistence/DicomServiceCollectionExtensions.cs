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
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
