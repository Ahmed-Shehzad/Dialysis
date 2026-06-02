using Dialysis.BuildingBlocks.Documents.Pdf;
using Dialysis.PDMS.Reporting.Contracts;

namespace Dialysis.PDMS.Reporting.Generators;

/// <summary>
/// Renders the human-readable billing summary that pairs with the
/// <see cref="DialysisSessionChargeReadyIntegrationEvent"/> emitted to EHR.Billing. The PDF is
/// what the operator stores in the patient chart; the integration event is what drives
/// the EDI 837 generation downstream (PR 4).
///
/// CPT mapping: in-centre haemodialysis 90935 (single eval) / 90937 (multi eval),
/// peritoneal dialysis 90945 / 90947. The generator picks the code based on the modality
/// and the per-session evaluation count; the EHR billing module owns the eventual claim.
/// </summary>
public sealed class BillingDocumentGenerator(IPdfDocumentRenderer pdf)
{
    public async Task<(byte[] Pdf, DialysisSessionChargeReadyIntegrationEvent ChargeEvent)> GenerateAsync(
        SessionReportContext context,
        int evaluationCount,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (evaluationCount < 1)
            throw new ArgumentOutOfRangeException(nameof(evaluationCount), "Evaluation count must be ≥ 1.");

        var cptCode = ResolveCptCode(context.Modality, evaluationCount);

        var sections = new List<DocumentSection>
        {
            new("Patient",
                [new KeyValueBlock(
                [
                    new KeyValuePair<string, string>("Name", context.PatientDisplayName),
                    new KeyValuePair<string, string>("MRN", context.MedicalRecordNumber),
                ])]),
            new("Charge",
                [new KeyValueBlock(
                [
                    new KeyValuePair<string, string>("CPT", cptCode),
                    new KeyValuePair<string, string>("Modality", context.Modality),
                    new KeyValuePair<string, string>("Duration", $"{context.DurationMinutes} min"),
                    new KeyValuePair<string, string>("Completed", context.CompletedAtUtc.ToString("u")),
                ])]),
        };
        var doc = new DocumentModel(
            Title: "Dialysis billing summary",
            Subtitle: cptCode,
            Sections: sections,
            Metadata: new Dictionary<string, string>
            {
                ["cpt"] = cptCode,
            });
        var bytes = await pdf.RenderAsync(doc, cancellationToken).ConfigureAwait(false);
        var chargeEvent = new DialysisSessionChargeReadyIntegrationEvent
        {
            SessionId = context.SessionId,
            PatientId = context.PatientId,
            Modality = context.Modality,
            DurationMinutes = context.DurationMinutes,
            CompletedAtUtc = context.CompletedAtUtc,
            CptCode = cptCode,
        };
        return (bytes, chargeEvent);
    }

    /// <summary>
    /// Returns the CPT code for the modality + evaluation count. The codes are stable; PR 4's
    /// EDI 837 writer reads the same mapping.
    /// </summary>
    public static string ResolveCptCode(string modality, int evaluationCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modality);
        var isHaemo = modality.Equals("HD", StringComparison.OrdinalIgnoreCase)
            || modality.Contains("haemo", StringComparison.OrdinalIgnoreCase)
            || modality.Contains("hemo", StringComparison.OrdinalIgnoreCase);
        var isPeritoneal = modality.Equals("PD", StringComparison.OrdinalIgnoreCase)
            || modality.Contains("peritoneal", StringComparison.OrdinalIgnoreCase);

        return (isHaemo, isPeritoneal, evaluationCount) switch
        {
            (true, _, 1) => "90935",
            (true, _, _) => "90937",
            (_, true, 1) => "90945",
            (_, true, _) => "90947",
            _ => throw new InvalidOperationException($"Unknown modality '{modality}' — cannot resolve CPT code."),
        };
    }
}
