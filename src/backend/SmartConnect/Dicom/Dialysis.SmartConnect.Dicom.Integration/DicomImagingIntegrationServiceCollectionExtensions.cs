using Dialysis.SmartConnect.Dicom.Ai;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Dicom.Integration;

/// <summary>
/// Opts a DICOM host into EHR imaging correlation: replaces the default no-op
/// <see cref="IImagingStudyLinkNotifier"/> with <see cref="TransponderImagingStudyLinkNotifier"/>,
/// which publishes <c>ImagingStudyLinkedIntegrationEvent</c> for STOW'd studies that carry an
/// accession number, and (when <c>Dicom:Ai:Enabled</c>) the gated AI finding event. Also wires the
/// AI pipeline (<see cref="ImagingAiServiceCollectionExtensions.AddImagingAi"/>) so the analyzer is
/// available — dormant unless the flag is on. Call order does not matter (replaces unconditionally).
/// </summary>
public static class DicomImagingIntegrationServiceCollectionExtensions
{
    public static IServiceCollection AddDicomImagingStudyLinkBridge(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddImagingAi(configuration);
        services.RemoveAll<IImagingStudyLinkNotifier>();
        services.AddScoped<IImagingStudyLinkNotifier, TransponderImagingStudyLinkNotifier>();
        return services;
    }
}
