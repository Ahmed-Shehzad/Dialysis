using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.SmartConnect.Dicom.Ai;

/// <summary>
/// Registers the AI imaging pipeline: options bound from <c>Dicom:Ai</c> (disabled by default),
/// the governed <see cref="ImagingAiAnalyzer"/>, the no-op audit sink, and the shipped sample
/// provider. A host swaps in a real model by registering its own <see cref="IImagingInferenceProvider"/>
/// before this call (TryAdd won't overwrite), and a real audit sink likewise.
/// </summary>
public static class ImagingAiServiceCollectionExtensions
{
    public static IServiceCollection AddImagingAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ImagingAiOptions>().Bind(configuration.GetSection(ImagingAiOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IImagingInferenceProvider, SampleHeuristicImagingInferenceProvider>();
        services.TryAddScoped<IImagingAiAuditSink, NoopImagingAiAuditSink>();
        services.TryAddScoped<ImagingAiAnalyzer>();
        return services;
    }
}
