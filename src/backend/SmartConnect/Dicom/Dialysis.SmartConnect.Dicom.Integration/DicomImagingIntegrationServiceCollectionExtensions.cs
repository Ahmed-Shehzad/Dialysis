using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Dicom.Integration;

/// <summary>
/// Opts a DICOM host into EHR imaging-order correlation: replaces the default no-op
/// <see cref="IImagingStudyLinkNotifier"/> with <see cref="TransponderImagingStudyLinkNotifier"/>,
/// which publishes <c>ImagingStudyLinkedIntegrationEvent</c> for STOW'd studies that carry an
/// accession number. Call this <em>before</em> <c>AddDicomIngestion</c> (which uses TryAdd), or after
/// with an explicit replace; this method replaces unconditionally so call order does not matter.
/// </summary>
public static class DicomImagingIntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddDicomImagingStudyLinkBridge(this IServiceCollection services)
    {
        services.RemoveAll<IImagingStudyLinkNotifier>();
        services.AddScoped<IImagingStudyLinkNotifier, TransponderImagingStudyLinkNotifier>();
        return services;
    }
}
