using Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;
using Dialysis.BuildingBlocks.Documents.Pdf.Graphics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Documents.Pdf;

/// <summary>
/// Composition entry-point. <c>AddPdfDocumentRendering()</c> registers the QuestPDF-backed
/// renderer as a singleton along with the AcroForms post-processor and the SkiaSharp-backed
/// graphics renderer (QR / barcode / Lottie). Production hosts that hold a commercial QuestPDF
/// licence override <see cref="QuestPdfLicensingOptions"/> in their composition root.
/// </summary>
public static class PdfServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPdfDocumentRendering()
        {
            // Collaborators are resolved by the renderer's injection ctor — register them as
            // singletons (both are stateless + thread-safe) so the same instances are reused.
            services.TryAddSingleton<IAcroFormProcessor, PdfSharpAcroFormProcessor>();
            services.TryAddSingleton<IDocumentGraphicsRenderer, SkiaDocumentGraphicsRenderer>();
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
