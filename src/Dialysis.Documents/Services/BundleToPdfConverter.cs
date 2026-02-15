using System.Text;
using Dialysis.ApiClients;
using Task = System.Threading.Tasks.Task;
using GdPicture14;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;

namespace Dialysis.Documents.Services;

/// <summary>Converts FHIR Document Bundle (Composition) to PDF using Nutrient .NET SDK (GdPicture).</summary>
public sealed class BundleToPdfConverter : IBundleToPdfConverter
{
    private readonly IFhirApi _fhirApi;
    private readonly ILogger<BundleToPdfConverter> _logger;

    private const float PageWidthMm = 210;
    private const float PageHeightMm = 297;
    private const float MarginMm = 20;
    private const float LineSpacingMm = 5;
    private const float HeaderFontSize = 14;
    private const float BodyFontSize = 11;

    public BundleToPdfConverter(IFhirApi fhirApi, ILogger<BundleToPdfConverter> logger)
    {
        _fhirApi = fhirApi;
        _logger = logger;
    }

    public Task<byte[]> ConvertAsync(Bundle bundle, CancellationToken cancellationToken = default)
    {
        var composition = bundle.Entry
            .Select(e => e.Resource as Composition)
            .FirstOrDefault(c => c != null);

        var binaryPdf = bundle.Entry
            .Select(e => e.Resource as Binary)
            .FirstOrDefault(b => b?.ContentType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true);

        if (binaryPdf != null && binaryPdf.Data != null)
        {
            return Task.FromResult(binaryPdf.Data);
        }

        var pdfBytes = composition != null
            ? BuildFromComposition(composition, bundle)
            : BuildFromBundle(bundle);

        return Task.FromResult(pdfBytes);
    }

    public async Task<byte[]> ConvertFromDocumentReferenceAsync(string documentReferenceId, CancellationToken cancellationToken = default)
    {
        var docRef = await _fhirApi.GetDocumentReference(documentReferenceId, cancellationToken);
        var attachment = docRef.Content?.FirstOrDefault()?.Attachment;
        if (attachment?.Url != null)
        {
            throw new NotSupportedException("DocumentReference with external URL not yet supported; use inline Binary.");
        }
        if (attachment?.Data != null)
        {
            var contentType = attachment.ContentType ?? "";
            if (contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
                return attachment.Data;
        }
        throw new InvalidOperationException("DocumentReference has no inline PDF content.");
    }

    private static byte[] BuildFromComposition(Composition composition, Bundle bundle)
    {
        var lines = new List<string>();

        if (composition.DateElement != null)
            lines.Add($"Date: {composition.Date}");
        if (composition.Author?.Any() == true)
            lines.Add($"Author: {string.Join(", ", composition.Author.Select(a => a.Display ?? a.Reference))}");

        foreach (var section in composition.Section ?? [])
        {
            lines.Add("");
            lines.Add(section.Title ?? "Section");
            if (section.Text != null && !string.IsNullOrEmpty(section.Text.Div))
            {
                lines.Add(section.Text.Div);
            }
            else if (section.Entry?.Any() == true)
            {
                foreach (var entry in section.Entry)
                {
                    var refId = entry.Reference?.Replace("#", "").Trim();
                    var resource = bundle.Entry
                        .FirstOrDefault(e => e.Resource?.Id == refId || e.FullUrl?.EndsWith(refId ?? "") == true)
                        ?.Resource;
                    if (resource != null)
                        lines.Add("  " + FormatResourceSummary(resource));
                }
            }
        }

        return BuildPdf(composition.Title ?? "FHIR Document", lines);
    }

    private static byte[] BuildFromBundle(Bundle bundle)
    {
        var lines = bundle.Entry
            .Where(e => e.Resource != null)
            .Select(e => $"{e.Resource!.TypeName} {e.Resource.Id}: {FormatResourceSummary(e.Resource)}")
            .ToList();
        return BuildPdf("FHIR Document Bundle", lines);
    }

    private static byte[] BuildPdf(string title, List<string> contentLines)
    {
        using var pdf = new GdPicturePDF();
        if (pdf.NewPDF() != GdPictureStatus.OK)
            throw new InvalidOperationException("Failed to create PDF.");

        pdf.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitMillimeter);
        pdf.SetOrigin(PdfOrigin.PdfOriginTopLeft);

        if (pdf.NewPage(PageWidthMm, PageHeightMm) != GdPictureStatus.OK)
            throw new InvalidOperationException("Failed to add PDF page.");

        var fontHelv = pdf.AddStandardFont(PdfStandardFont.PdfStandardFontHelvetica);
        var fontHelvBold = pdf.AddStandardFont(PdfStandardFont.PdfStandardFontHelveticaBold);
        if (string.IsNullOrEmpty(fontHelv) || string.IsNullOrEmpty(fontHelvBold))
            throw new InvalidOperationException("Failed to add fonts.");

        float y = MarginMm;

        pdf.SetTextSize(HeaderFontSize);
        pdf.DrawTextBox(fontHelvBold, MarginMm, y, PageWidthMm - 2 * MarginMm, 10,
            TextAlignment.TextAlignmentNear, TextAlignment.TextAlignmentNear, title);
        y += 12;

        pdf.SetTextSize(BodyFontSize);

        foreach (var line in contentLines)
        {
            if (y > PageHeightMm - MarginMm - 10)
                break;
            pdf.DrawText(fontHelv, MarginMm, y, line ?? "");
            y += BodyFontSize * 0.4f + LineSpacingMm;
        }

        using var stream = new MemoryStream();
        pdf.SaveToStream(stream, false, false);
        return stream.ToArray();
    }

    private static string FormatResourceSummary(Resource resource)
    {
        return resource switch
        {
            Patient p => FormatPatientName(p),
            Encounter e => $"{e.Id} {e.Period?.Start}",
            _ => resource.Id ?? resource.TypeName
        };
    }

    private static string FormatPatientName(Patient patient)
    {
        if (patient.Name?.Any() != true) return patient.Id ?? "";
        var n = patient.Name.First();
        var parts = new List<string> { n.Family ?? "" };
        if (n.Given?.Any() == true) parts.AddRange(n.Given.Where(g => g != null)!);
        return string.Join(" ", parts.Where(s => !string.IsNullOrEmpty(s)));
    }
}
