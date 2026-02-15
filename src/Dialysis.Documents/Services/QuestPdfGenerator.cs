using System.Text;
using Dialysis.ApiClients;
using Dialysis.Documents.Configuration;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Dialysis.Documents.Services;

/// <summary>Generates PDF from FHIR using QuestPDF.</summary>
public sealed class QuestPdfGenerator : IPdfGenerator
{
    private readonly IFhirApi _fhirApi;
    private readonly DocumentsOptions _options;
    private readonly ILogger<QuestPdfGenerator> _logger;

    public QuestPdfGenerator(
        IFhirApi fhirApi,
        IOptions<DocumentsOptions> options,
        ILogger<QuestPdfGenerator> logger)
    {
        _fhirApi = fhirApi;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<byte[]> GenerateAsync(
        string template,
        string? patientId,
        string? encounterId,
        string? resourceId,
        Bundle? bundle,
        CancellationToken cancellationToken = default)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Patient? patient = null;
        Encounter? encounter = null;
        MeasureReport? measureReport = null;

        if (bundle != null)
        {
            patient = bundle.Entry
                .Select(e => e.Resource as Patient)
                .FirstOrDefault(p => p != null);
            encounter = bundle.Entry
                .Select(e => e.Resource as Encounter)
                .FirstOrDefault(e => e != null);
            measureReport = bundle.Entry
                .Select(e => e.Resource as MeasureReport)
                .FirstOrDefault(m => m != null);
        }
        else if (!string.IsNullOrEmpty(patientId))
        {
            patient = await _fhirApi.GetPatient(patientId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(encounterId) && encounter == null)
        {
            encounter = await _fhirApi.GetEncounter(encounterId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(resourceId) && template.Equals("measure-report", StringComparison.OrdinalIgnoreCase))
        {
            measureReport = await _fhirApi.GetMeasureReport(resourceId, cancellationToken);
        }

        var doc = template.ToLowerInvariant() switch
        {
            "session-summary" => BuildSessionSummary(patient, encounter),
            "patient-summary" => BuildPatientSummary(patient),
            "measure-report" => BuildMeasureReport(measureReport),
            _ => BuildGenericReport(template, patient, encounter, measureReport)
        };

        using var stream = new MemoryStream();
        doc.GeneratePdf(stream);
        return stream.ToArray();
    }

    private static IDocument BuildSessionSummary(Patient? patient, Encounter? encounter)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text("Dialysis Session Summary")
                    .SemiBold().FontSize(18).FontColor(Colors.Blue.Medium);

                page.Content().Column(column =>
                {
                    column.Spacing(10);

                    if (patient != null)
                    {
                        column.Item().Text("Patient").SemiBold();
                        column.Item().Text(FormatPatientName(patient));
                        if (patient.BirthDateElement != null)
                            column.Item().Text($"DOB: {patient.BirthDate}");
                    }

                    if (encounter != null)
                    {
                        column.Item().Text("Encounter").SemiBold();
                        column.Item().Text($"ID: {encounter.Id}");
                        if (encounter.Period != null)
                            column.Item().Text($"Period: {encounter.Period.Start} – {encounter.Period.End}");
                    }

                    column.Item().Text("--- End of Summary ---");
                });
            });
        });
    }

    private static IDocument BuildPatientSummary(Patient? patient)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text("Patient Summary")
                    .SemiBold().FontSize(18).FontColor(Colors.Blue.Medium);

                page.Content().Column(column =>
                {
                    column.Spacing(10);
                    if (patient != null)
                    {
                        column.Item().Text(FormatPatientName(patient));
                        if (patient.BirthDateElement != null)
                            column.Item().Text($"Date of birth: {patient.BirthDate}");
                        if (patient.Identifier.Any())
                            column.Item().Text($"Identifiers: {string.Join(", ", patient.Identifier.Select(i => $"{i.System}: {i.Value}"))}");
                    }
                    else
                    {
                        column.Item().Text("No patient data available.");
                    }
                });
            });
        });
    }

    private static IDocument BuildMeasureReport(MeasureReport? report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text("Measure Report")
                    .SemiBold().FontSize(18).FontColor(Colors.Blue.Medium);

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    if (report != null)
                    {
                        column.Item().Text($"Measure: {report.Measure}");
                        column.Item().Text($"Status: {report.Status}");
                        column.Item().Text($"Date: {report.Date}");
                        if (report.Group.Any())
                        {
                            column.Item().Text("Groups:").SemiBold();
                            foreach (var g in report.Group)
                            {
                                column.Item().PaddingLeft(10).Text($"Population: {g.Population?.FirstOrDefault()?.Code?.ToString() ?? "n/a"}");
                            }
                        }
                    }
                    else
                    {
                        column.Item().Text("No measure report data available.");
                    }
                });
            });
        });
    }

    private static IDocument BuildGenericReport(
        string template,
        Patient? patient,
        Encounter? encounter,
        MeasureReport? measureReport)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text($"Report: {template}")
                    .SemiBold().FontSize(18);

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    if (patient != null) column.Item().Text($"Patient: {FormatPatientName(patient)}");
                    if (encounter != null) column.Item().Text($"Encounter: {encounter.Id}");
                    if (measureReport != null) column.Item().Text($"MeasureReport: {measureReport.Measure}");
                });
            });
        });
    }

    private static string FormatPatientName(Patient patient)
    {
        if (patient.Name?.Any() != true) return patient.Id ?? "Unknown";
        var n = patient.Name.First();
        var sb = new StringBuilder();
        if (n.Family != null) sb.Append(n.Family);
        if (n.Given?.Any() == true) sb.Append(", ").Append(string.Join(" ", n.Given));
        return sb.Length > 0 ? sb.ToString() : patient.Id ?? "Unknown";
    }
}
