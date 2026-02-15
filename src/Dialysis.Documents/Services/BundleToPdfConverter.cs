using System.Text;
using Dialysis.ApiClients;
using Task = System.Threading.Tasks.Task;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dialysis.Documents.Services;

/// <summary>Converts FHIR Document Bundle (Composition) to PDF using QuestPDF.</summary>
public sealed class BundleToPdfConverter : IBundleToPdfConverter
{
    private readonly IFhirApi _fhirApi;
    private readonly ILogger<BundleToPdfConverter> _logger;

    public BundleToPdfConverter(IFhirApi fhirApi, ILogger<BundleToPdfConverter> logger)
    {
        _fhirApi = fhirApi;
        _logger = logger;
    }

    public Task<byte[]> ConvertAsync(Bundle bundle, CancellationToken cancellationToken = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

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

        var doc = composition != null
            ? BuildFromComposition(composition, bundle)
            : BuildFromBundle(bundle);

        using var stream = new MemoryStream();
        doc.GeneratePdf(stream);
        return Task.FromResult(stream.ToArray());
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

    private static IDocument BuildFromComposition(Composition composition, Bundle bundle)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text(composition.Title ?? "FHIR Document")
                    .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    if (composition.DateElement != null)
                        column.Item().Text($"Date: {composition.Date}");
                    if (composition.Author?.Any() == true)
                        column.Item().Text($"Author: {string.Join(", ", composition.Author.Select(a => a.Display ?? a.Reference))}");

                    foreach (var section in composition.Section ?? [])
                    {
                        column.Item().Text(section.Title ?? "Section").SemiBold();
                        if (section.Text != null)
                            column.Item().Text(section.Text.Div);
                        else if (section.Entry?.Any() == true)
                        {
                            foreach (var entry in section.Entry)
                            {
                                var refId = entry.Reference?.Replace("#", "").Trim();
                                var resource = bundle.Entry
                                    .FirstOrDefault(e => e.Resource?.Id == refId || e.FullUrl?.EndsWith(refId ?? "") == true)
                                    ?.Resource;
                                if (resource != null)
                                    column.Item().PaddingLeft(10).Text(FormatResourceSummary(resource));
                            }
                        }
                        column.Item().Height(10);
                    }
                });
            });
        });
    }

    private static IDocument BuildFromBundle(Bundle bundle)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text("FHIR Document Bundle")
                    .SemiBold().FontSize(16);

                page.Content().Column(column =>
                {
                    foreach (var entry in bundle.Entry)
                    {
                        if (entry.Resource != null)
                            column.Item().Text($"{entry.Resource.TypeName} {entry.Resource.Id}: {FormatResourceSummary(entry.Resource)}");
                    }
                });
            });
        });
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
