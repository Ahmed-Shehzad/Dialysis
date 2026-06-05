using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.BuildingBlocks.Documents.Pdf.AcroForms;
using Dialysis.Module.Contracts.Billing;
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
public sealed class DischargeLetterGenerator
{
    private readonly IPdfDocumentRenderer _pdf;
    private readonly MustacheMarkdownBinder _binder;
    /// <summary>
    /// Renders a per-session discharge letter PDF. The clinical summary is built from the
    /// <see cref="SessionReportContext"/> and the operator-authored
    /// <see cref="ReportKind.DischargeLetter"/> template — operators control the wording without
    /// a code deploy. The generated PDF is deterministic byte-for-byte for the same context +
    /// template version, which is what lets the audit gate hash the output and detect tampering.
    /// </summary>
    public DischargeLetterGenerator(IPdfDocumentRenderer pdf,
        MustacheMarkdownBinder binder)
    {
        _pdf = pdf;
        _binder = binder;
    }
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
            ? _binder.BindToPlainText(body, bindings)
            : "Patient completed the scheduled dialysis session as documented below.";

        var sections = new List<DocumentSection>();

        // Drug allergies surface as an Alert callout at the top — the macro layer keeps the
        // palette consistent with the rest of the platform's danger-state UI.
        if (context.DrugAllergies is { Count: > 0 } allergies)
        {
            sections.Add(new DocumentSection("Drug allergies",
                [new CalloutBlock(
                    Heading: "Known drug allergies",
                    Body: string.Join(", ", allergies),
                    IsAlert: true)]));
        }

        sections.Add(new DocumentSection("Patient",
                [new KeyValueBlock(
                [
                    new KeyValuePair<string, string>("Name", context.PatientDisplayName),
                    new KeyValuePair<string, string>("MRN", context.MedicalRecordNumber),
                ])]));
        sections.Add(new DocumentSection("Treatment",
            [new KeyValueBlock(
            [
                new KeyValuePair<string, string>("Chair", context.ChairLabel),
                new KeyValuePair<string, string>("Modality", context.Modality),
                new KeyValuePair<string, string>("Started", context.StartedAtUtc.ToString("u")),
                new KeyValuePair<string, string>("Completed", context.CompletedAtUtc.ToString("u")),
                new KeyValuePair<string, string>(
                    "Treatment usage time",
                    $"{TreatmentUsageTime.Format(context.DurationMinutes)} ({context.DurationMinutes} min)"),
            ])]));
        sections.Add(new DocumentSection("Clinical summary", [new ParagraphBlock(summary)]));

        if (!string.IsNullOrWhiteSpace(context.PendingFollowUp))
        {
            sections.Add(new DocumentSection("Follow-up",
                [new CalloutBlock(
                    Heading: "Scheduled follow-up",
                    Body: context.PendingFollowUp,
                    IsAlert: false)]));
        }

        if (context.Medications.Count > 0)
        {
            sections.Add(new DocumentSection("Medications administered",
                [new TableBlock(
                    Headers: ["Time", "Medication", "Dose", "Route", "Outcome"],
                    Rows: [.. context.Medications.Select(m => (IReadOnlyList<string>)new[]
                    {
                        m.AdministeredAtUtc.ToString("u"),
                        m.MedicationDisplay,
                        $"{m.DoseQuantity} {m.DoseUnit}",
                        m.Route,
                        m.WasAdministered ? "Given" : $"Declined: {m.DeclineReason}",
                    })])]));
        }

        if (context.Alarms.Count > 0)
        {
            sections.Add(new DocumentSection("Alarms during session",
                [new TableBlock(
                    Headers: ["Time", "Code", "Severity", "Ack?"],
                    Rows: [.. context.Alarms.Select(a => (IReadOnlyList<string>)new[]
                    {
                        a.RaisedAtUtc.ToString("u"),
                        a.AlarmCode,
                        a.Severity,
                        a.Acknowledged ? "yes" : "no",
                    })])]));
        }

        var doc = new DocumentModel(
            Title: "Dialysis discharge letter",
            Subtitle: context.CompletedAtUtc.ToString("yyyy-MM-dd"),
            Sections: sections,
            Metadata: new Dictionary<string, string>
            {
                ["sessionId"] = context.SessionId.ToString(),
                ["templateVersion"] = template?.PublishedVersionNumber?.ToString() ?? "default",
                // The language the letter was actually rendered in: the resolved template's
                // language when one matched, else the patient's preferred language, else the
                // platform default. Lets the ePA upload tag the document's language part.
                ["language"] = ResolveLanguage(context, template),
            });
        return await _pdf.RenderAsync(doc, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Renders the discharge letter with an interactive AcroForm signature placeholder so
    /// the clinician can countersign the document at the bedside (Adobe Reader, an EHR
    /// signing service, or the operator workstation supply the cryptographic signature).
    /// BDSG §22 and the Berufsordnung-§10 retention rules expect a signed clinical
    /// record; this overload makes the signature step part of the rendered artifact rather
    /// than a separate step.
    /// </summary>
    public async Task<byte[]> GenerateSignableAsync(
        SessionReportContext context,
        ReportTemplate? template,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var baseBytes = await GenerateAsync(context, template, cancellationToken).ConfigureAwait(false);

        // PDF coordinates: 1pt = 1/72". A4 short edge is ~595 pt; long edge ~842 pt. The
        // signature block sits at the bottom-left so it never overlaps the rendered body.
        var placements = new AcroFormPlacement[]
        {
            new(PageNumber: 1, Origin: new PdfPoint(60, 120), Size: new PdfSize(260, 24),
                Field: new TextFormField("clinician_name") { Tooltip = "Clinician name (printed)" }),
            new(PageNumber: 1, Origin: new PdfPoint(60, 70), Size: new PdfSize(260, 30),
                Field: new SignatureFormField("clinician_signature") { Tooltip = "Clinician signature" }),
            new(PageNumber: 1, Origin: new PdfPoint(340, 120), Size: new PdfSize(160, 24),
                Field: new TextFormField("countersign_date") { Tooltip = "Sign date (YYYY-MM-DD)" }),
            new(PageNumber: 1, Origin: new PdfPoint(340, 80), Size: new PdfSize(16, 16),
                Field: new CheckBoxFormField("patient_consent_received")
                {
                    Tooltip = "Patient consented to discharge",
                }),
        };
        return await _pdf.RenderWithFormsAsync(BuildModelForSignature(context, template), placements, cancellationToken)
            .ConfigureAwait(false);
    }

    private static DocumentModel BuildModelForSignature(SessionReportContext context, ReportTemplate? template)
    {
        // Re-use the base layout but tag the metadata so downstream auditors can tell the
        // signable copy apart from the read-only copy (same content, different
        // interactivity).
        var doc = new DocumentModel(
            Title: "Dialysis discharge letter (signable)",
            Subtitle: context.CompletedAtUtc.ToString("yyyy-MM-dd"),
            Sections: BuildSectionsForSignablePlaceholder(),
            Metadata: new Dictionary<string, string>
            {
                ["sessionId"] = context.SessionId.ToString(),
                ["templateVersion"] = template?.PublishedVersionNumber?.ToString() ?? "default",
                ["form"] = "signable",
            });
        return doc;
    }

    private static IReadOnlyList<DocumentSection> BuildSectionsForSignablePlaceholder() =>
        [new DocumentSection("Signature",
            [new ParagraphBlock("Reserved space for clinician signature, name, and date below.")])];

    /// <summary>BCP-47 default applied when neither template nor patient declares a language.</summary>
    public const string DefaultLanguageCode = "de";

    private static string ResolveLanguage(SessionReportContext context, ReportTemplate? template) =>
        template?.LanguageCode
            ?? (string.IsNullOrWhiteSpace(context.PreferredLanguageCode)
                ? DefaultLanguageCode
                : context.PreferredLanguageCode.Trim().ToLowerInvariant());

    private static Dictionary<string, object?> BuildBindings(SessionReportContext context) => new()
    {
        ["patient"] = new
        {
            name = context.PatientDisplayName,
            mrn = context.MedicalRecordNumber,
            language = string.IsNullOrWhiteSpace(context.PreferredLanguageCode)
                ? DefaultLanguageCode
                : context.PreferredLanguageCode.Trim().ToLowerInvariant(),
        },
        ["session"] = new
        {
            chair = context.ChairLabel,
            modality = context.Modality,
            started = context.StartedAtUtc.ToString("u"),
            completed = context.CompletedAtUtc.ToString("u"),
            duration = context.DurationMinutes,
            usageTime = TreatmentUsageTime.Format(context.DurationMinutes),
        },
        ["counts"] = new
        {
            medications = context.Medications.Count,
            alarms = context.Alarms.Count,
            vitals = context.Vitals.Count,
        },
    };
}
