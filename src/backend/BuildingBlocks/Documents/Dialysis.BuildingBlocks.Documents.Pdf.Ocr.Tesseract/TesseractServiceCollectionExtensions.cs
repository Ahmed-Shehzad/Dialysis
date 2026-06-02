using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Dialysis.BuildingBlocks.Documents.Pdf.Ocr.Tesseract;

/// <summary>
/// Composition entry-point. <c>AddTesseractOcrEngine(configuration)</c> binds the
/// trained-data path from configuration and registers the engine as the
/// <see cref="IOcrEngine"/> the OCR-augmented extractor consumes.
/// </summary>
public static class TesseractServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddTesseractOcrEngine(IConfiguration configuration, string sectionName = "Ocr:Tesseract")
        {
            ArgumentNullException.ThrowIfNull(configuration);
            services.AddOptions<TesseractOcrOptions>().Bind(configuration.GetSection(sectionName));
            services.TryAddSingleton<IOcrEngine, TesseractOcrEngine>();
            return services;
        }
    }
}
