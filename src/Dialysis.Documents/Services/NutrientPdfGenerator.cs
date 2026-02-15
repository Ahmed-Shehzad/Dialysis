using System.Text;
using Dialysis.ApiClients;
using Dialysis.Documents.Configuration;
using GdPicture14;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.Documents.Services;

/// <summary>Generates PDF from FHIR using Nutrient .NET SDK (GdPicture).</summary>
public sealed class NutrientPdfGenerator : IPdfGenerator
{
    private readonly IFhirApi _fhirApi;
    private readonly DocumentsOptions _options;
    private readonly ILogger<NutrientPdfGenerator> _logger;

    private const float PageWidthMm = 210;
    private const float PageHeightMm = 297;
    private const float MarginMm = 20;
    private const float LineSpacingMm = 5;
    private const float HeaderFontSize = 14;
    private const float BodyFontSize = 11;

    public NutrientPdfGenerator(
        IFhirApi fhirApi,
        IOptions<DocumentsOptions> options,
        ILogger<NutrientPdfGenerator> logger)
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

        return template.ToLowerInvariant() switch
        {
            "session-summary" => BuildSessionSummary(patient, encounter),
            "patient-summary" => BuildPatientSummary(patient),
            "measure-report" => BuildMeasureReport(measureReport),
            _ => BuildGenericReport(template, patient, encounter, measureReport)
        };
    }

    private static byte[] BuildSessionSummary(Patient? patient, Encounter? encounter)
    {
        var lines = new List<string>
        {
            "Dialysis Session Summary",
            "",
            "Patient",
            patient != null ? FormatPatientName(patient) : "—",
            "",
            "Encounter",
            encounter != null ? $"ID: {encounter.Id}" : "—",
            "",
            "--- End of Summary ---"
        };
        if (patient?.BirthDateElement != null)
            lines.Insert(4, $"DOB: {patient.BirthDate}");
        if (encounter?.Period != null)
            lines.Insert(8, $"Period: {encounter.Period.Start} – {encounter.Period.End}");
        return BuildPdf("Dialysis Session Summary", lines);
    }

    private static byte[] BuildPatientSummary(Patient? patient)
    {
        var lines = new List<string>();
        if (patient != null)
        {
            lines.Add(FormatPatientName(patient));
            if (patient.BirthDateElement != null)
                lines.Add($"Date of birth: {patient.BirthDate}");
            if (patient.Identifier.Any())
                lines.Add($"Identifiers: {string.Join(", ", patient.Identifier.Select(i => $"{i.System}: {i.Value}"))}");
        }
        else
        {
            lines.Add("No patient data available.");
        }
        return BuildPdf("Patient Summary", lines);
    }

    private static byte[] BuildMeasureReport(MeasureReport? report)
    {
        var lines = new List<string>();
        if (report != null)
        {
            lines.Add($"Measure: {report.Measure}");
            lines.Add($"Status: {report.Status}");
            lines.Add($"Date: {report.Date}");
            if (report.Group.Any())
            {
                lines.Add("Groups:");
                foreach (var g in report.Group)
                {
                    lines.Add($"  Population: {g.Population?.FirstOrDefault()?.Code?.ToString() ?? "n/a"}");
                }
            }
        }
        else
        {
            lines.Add("No measure report data available.");
        }
        return BuildPdf("Measure Report", lines);
    }

    private static byte[] BuildGenericReport(
        string template,
        Patient? patient,
        Encounter? encounter,
        MeasureReport? measureReport)
    {
        var lines = new List<string>();
        if (patient != null) lines.Add($"Patient: {FormatPatientName(patient)}");
        if (encounter != null) lines.Add($"Encounter: {encounter.Id}");
        if (measureReport != null) lines.Add($"MeasureReport: {measureReport.Measure}");
        return BuildPdf($"Report: {template}", lines);
    }

    private static byte[] BuildPdf(string title, IEnumerable<string> contentLines)
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
