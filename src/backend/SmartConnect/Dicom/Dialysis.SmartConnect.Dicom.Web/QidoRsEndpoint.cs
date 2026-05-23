using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Dialysis.SmartConnect.Dicom.Web;

/// <summary>
/// DICOMweb QIDO-RS query endpoint (PS3.18 §10.6). <c>GET /studies?PatientID=...&amp;StudyDate=...</c>
/// returns the matching studies as JSON in the standard DICOM-JSON format (a subset; full
/// DICOM-JSON is far richer than the few fields a typical clinician search needs).
/// </summary>
/// <remarks>
/// The DICOMweb spec uses GET parameters keyed by DICOM tag name (<c>PatientID</c>,
/// <c>StudyDate</c>) rather than the conventional REST snake_case; we honour both since the
/// internal SPA may use either.
/// </remarks>
public static class QidoRsEndpoint
{
    public static IEndpointRouteBuilder MapQidoRs(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/studies", HandleSearchAsync);
        return endpoints;
    }

    /// <summary>Study-level QIDO-RS query.</summary>
    public static async Task<IResult> HandleSearchAsync(
        [FromServices] IDicomInstanceStore instances,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var patientId = request.Query["PatientID"].FirstOrDefault();
        var studyDateRange = request.Query["StudyDate"].FirstOrDefault();
        var (from, to) = ParseDateRange(studyDateRange);

        var studies = await instances.SearchStudiesAsync(patientId, from, to, cancellationToken).ConfigureAwait(false);

        // DICOM-JSON shape: tag-keyed objects. The minimum a QIDO-RS client expects per study is
        // 0020,000D (StudyInstanceUID) and 0010,0020 (PatientID). Vendor-specific clients may
        // demand more — extend as those needs land.
        return Results.Json(studies.Select(s => new Dictionary<string, object?>
        {
            ["0020000D"] = new { vr = "UI", Value = new[] { s.StudyInstanceUid } },
            ["00100010"] = new { vr = "PN", Value = new[] { new { Alphabetic = s.PatientName ?? "" } } },
            ["00100020"] = new { vr = "LO", Value = new[] { s.PatientId ?? "" } },
            ["00080050"] = new { vr = "SH", Value = Array.Empty<string>() }, // AccessionNumber
            ["00201208"] = new { vr = "IS", Value = new[] { s.Series.Sum(se => se.Instances.Count) } },
        }));
    }

    private static (DateTimeOffset? From, DateTimeOffset? To) ParseDateRange(string? range)
    {
        if (string.IsNullOrEmpty(range)) return (null, null);

        // DICOM date range syntax: "YYYYMMDD-YYYYMMDD", "YYYYMMDD-" (open right), "-YYYYMMDD"
        // (open left), or "YYYYMMDD" (single day).
        var dash = range.IndexOf('-', StringComparison.Ordinal);
        if (dash < 0)
        {
            var day = TryParseDicomDate(range);
            return (day, day?.AddDays(1).AddTicks(-1));
        }
        var fromStr = range[..dash];
        var toStr = range[(dash + 1)..];
        return (TryParseDicomDate(fromStr), TryParseDicomDate(toStr));
    }

    private static DateTimeOffset? TryParseDicomDate(string s) =>
        DateTimeOffset.TryParseExact(
            s, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal, out var dt) ? dt : null;
}
