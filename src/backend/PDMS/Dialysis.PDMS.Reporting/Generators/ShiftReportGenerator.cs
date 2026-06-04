using Dialysis.BuildingBlocks.Documents.Pdf;

namespace Dialysis.PDMS.Reporting.Generators;

/// <summary>
/// Per-chair / per-shift roll-up. Produces one PDF that lists every session that started or
/// ended in the window, with totals for alarms + medications + duration. Operators print this
/// at handover; the generator stays read-only so it can be re-run any number of times for the
/// same window with the same byte output.
/// </summary>
public sealed class ShiftReportGenerator
{
    private readonly IPdfDocumentRenderer _pdf;
    /// <summary>
    /// Per-chair / per-shift roll-up. Produces one PDF that lists every session that started or
    /// ended in the window, with totals for alarms + medications + duration. Operators print this
    /// at handover; the generator stays read-only so it can be re-run any number of times for the
    /// same window with the same byte output.
    /// </summary>
    public ShiftReportGenerator(IPdfDocumentRenderer pdf) => _pdf = pdf;
    public async Task<byte[]> GenerateAsync(
        ShiftReportContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var rows = context.Sessions.Select(s => (IReadOnlyList<string>)new[]
        {
            s.PatientDisplayName,
            s.ChairLabel,
            s.StartedAtUtc.ToString("HH:mm"),
            s.CompletedAtUtc.ToString("HH:mm"),
            $"{s.DurationMinutes} min",
            s.Medications.Count.ToString(),
            s.Alarms.Count.ToString(),
        }).ToArray();

        var sections = new List<DocumentSection>
        {
            new("Shift window",
                [new KeyValueBlock(
                [
                    new KeyValuePair<string, string>("Shift", context.ShiftLabel),
                    new KeyValuePair<string, string>("From", context.WindowStartUtc.ToString("u")),
                    new KeyValuePair<string, string>("Until", context.WindowEndUtc.ToString("u")),
                    new KeyValuePair<string, string>("Sessions", context.Sessions.Count.ToString()),
                ])]),
            new("Sessions",
                [new TableBlock(
                    Headers: ["Patient", "Chair", "Start", "End", "Duration", "Meds", "Alarms"],
                    Rows: rows)]),
        };
        var doc = new DocumentModel(
            Title: "Dialysis shift report",
            Subtitle: $"{context.ShiftLabel} — {context.WindowStartUtc:yyyy-MM-dd}",
            Sections: sections,
            Metadata: new Dictionary<string, string>
            {
                ["shift"] = context.ShiftLabel,
            });
        return await _pdf.RenderAsync(doc, cancellationToken).ConfigureAwait(false);
    }
}

public sealed record ShiftReportContext
{
    public ShiftReportContext(string ShiftLabel,
        DateTime WindowStartUtc,
        DateTime WindowEndUtc,
        IReadOnlyList<SessionReportContext> Sessions)
    {
        this.ShiftLabel = ShiftLabel;
        this.WindowStartUtc = WindowStartUtc;
        this.WindowEndUtc = WindowEndUtc;
        this.Sessions = Sessions;
    }
    public string ShiftLabel { get; init; }
    public DateTime WindowStartUtc { get; init; }
    public DateTime WindowEndUtc { get; init; }
    public IReadOnlyList<SessionReportContext> Sessions { get; init; }
    public void Deconstruct(out string shiftLabel, out DateTime windowStartUtc, out DateTime windowEndUtc, out IReadOnlyList<SessionReportContext> sessions)
    {
        shiftLabel = this.ShiftLabel;
        windowStartUtc = this.WindowStartUtc;
        windowEndUtc = this.WindowEndUtc;
        sessions = this.Sessions;
    }
}
