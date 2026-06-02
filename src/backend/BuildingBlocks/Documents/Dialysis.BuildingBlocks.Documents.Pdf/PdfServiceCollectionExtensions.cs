using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Documents.Pdf;

/// <summary>
/// Composition entry-point. <c>AddPdfDocumentRendering()</c> registers the QuestPDF-backed
/// renderer as a singleton. Production hosts that hold a commercial QuestPDF licence override
/// <see cref="QuestPdfLicensingOptions"/> in their composition root.
/// </summary>
public static class PdfServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPdfDocumentRendering()
        {
            services.TryAddSingleton<IPdfDocumentRenderer, QuestPdfDocumentRenderer>();
            return services;
        }
    }
}

public sealed class QuestPdfLicensingOptions
{
    public bool UseCommercialLicence { get; set; }
    public string? CommercialLicenceKey { get; set; }
}
