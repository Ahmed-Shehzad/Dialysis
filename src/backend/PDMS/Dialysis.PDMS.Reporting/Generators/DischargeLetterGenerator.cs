using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.PDMS.Reporting.Domain;
using Dialysis.PDMS.Reporting.Templating;

namespace Dialysis.PDMS.Reporting.Generators;

/// <summary>
/// Renders a per-session discharge letter PDF. The clinical summary is built from the
/// <see cref="SessionReportContext"/> and the operator-authored
/// <see cref="ReportKind.DischargeLetter"/> template — operators control the wording without
/// a code deploy. The generated PDF is deterministic byte-for-byte for the same context +
/// template version, which is what lets the audit gate hash the output and detect tampering.
/// </summary>
public sealed class DischargeLetterGenerator(
    IPdfDocumentRenderer pdf,
    MustacheMarkdownBinder binder)
{
    public async Task<byte[]> GenerateAsync(
        SessionReportContext context,
        ReportTemplate? template,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var bindings = BuildBindings(context);

        // Operator-authored summary if a template is published; fall back to a fixed body
        // so an unconfigured deployment still produces a usable letter.
        var summary = template?.GetPublishedBody() is { } body
            ? binder.BindToPlainText(body, bindings)
            : "Patient completed the scheduled dialysis session as documented below.";

        var sections = new List<DocumentSection>
        {
            new("Patient",
                [new KeyValueBlock(
                [
                    new KeyValuePair<string, string>("Name", context.PatientDisplayName),
                    new KeyValuePair<string, string>("MRN", context.MedicalRecordNumber),
                ])]),
            new("Treatment",
                [new KeyValueBlock(
                [
                    new KeyValuePair<string, string>("Chair", context.ChairLabel),
                    new KeyValuePair<string, string>("Modality", context.Modality),
                    new KeyValuePair<string, string>("Started", context.StartedAtUtc.ToString("u")),
                    new KeyValuePair<string, string>("Completed", context.CompletedAtUtc.ToString("u")),
                    new KeyValuePair<string, string>("Duration", $"{context.DurationMinutes} min"),
                ])]),
            new("Clinical summary", [new ParagraphBlock(summary)]),
        };

        if (context.Medications.Count > 0)
        {
            sections.Add(new DocumentSection("Medications administered",
                [new TableBlock(
                    Headers: ["Time", "Medication", "Dose", "Route", "Outcome"],
                    Rows: context.Medications.Select(m => (IReadOnlyList<string>)new[]
                    {
                        m.AdministeredAtUtc.ToString("u"),
                        m.MedicationDisplay,
                        $"{m.DoseQuantity} {m.DoseUnit}",
                        m.Route,
                        m.WasAdministered ? "Given" : $"Declined: {m.DeclineReason}",
                    }).ToArray())]));
        }

        if (context.Alarms.Count > 0)
        {
            sections.Add(new DocumentSection("Alarms during session",
                [new TableBlock(
                    Headers: ["Time", "Code", "Severity", "Ack?"],
                    Rows: context.Alarms.Select(a => (IReadOnlyList<string>)new[]
                    {
                        a.RaisedAtUtc.ToString("u"),
                        a.AlarmCode,
                        a.Severity,
                        a.Acknowledged ? "yes" : "no",
                    }).ToArray())]));
        }

        var doc = new DocumentModel(
            Title: "Dialysis discharge letter",
            Subtitle: context.CompletedAtUtc.ToString("yyyy-MM-dd"),
            Sections: sections,
            Metadata: new Dictionary<string, string>
            {
                ["sessionId"] = context.SessionId.ToString(),
                ["templateVersion"] = template?.PublishedVersionNumber?.ToString() ?? "default",
            });
        return await pdf.RenderAsync(doc, cancellationToken).ConfigureAwait(false);
    }

    private static Dictionary<string, object?> BuildBindings(SessionReportContext context) => new()
    {
        ["patient"] = new
        {
            name = context.PatientDisplayName,
            mrn = context.MedicalRecordNumber,
        },
        ["session"] = new
        {
            chair = context.ChairLabel,
            modality = context.Modality,
            started = context.StartedAtUtc.ToString("u"),
            completed = context.CompletedAtUtc.ToString("u"),
            duration = context.DurationMinutes,
        },
        ["counts"] = new
        {
            medications = context.Medications.Count,
            alarms = context.Alarms.Count,
            vitals = context.Vitals.Count,
        },
    };
}
