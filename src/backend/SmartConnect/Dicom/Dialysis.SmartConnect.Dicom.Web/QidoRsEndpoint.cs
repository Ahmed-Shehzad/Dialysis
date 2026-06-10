using System.Globalization;
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
        endpoints.MapGet("/studies/{studyUid}/instances", HandleStudyInstancesAsync);
        return endpoints;
    }

    /// <summary>
    /// Instance-level QIDO-RS query: every instance under a study (series + SOP UIDs, ordinal instance
    /// number, modality), ordered by series then SOP. Backs the chart viewer's instance paging.
    /// </summary>
    public static async Task<IResult> HandleStudyInstancesAsync(
        string studyUid,
        [FromServices] IDicomInstanceStore instances,
        CancellationToken cancellationToken)
    {
        var list = await instances.GetByStudyAsync(studyUid, cancellationToken).ConfigureAwait(false);
        return Results.Json(list
            .Select((m, index) => new Dictionary<string, object?>
            {
                ["0020000D"] = new { vr = "UI", Value = new[] { m.StudyInstanceUid } },
                ["0020000E"] = new { vr = "UI", Value = new[] { m.SeriesInstanceUid } },
                ["00080018"] = new { vr = "UI", Value = new[] { m.SopInstanceUid } },
                ["00200013"] = new { vr = "IS", Value = new[] { index + 1 } }, // ordinal instance number
                ["00080060"] = new { vr = "CS", Value = new[] { m.Modality ?? "" } },
            })
            .ToArray());
    }

    /// <summary>Study-level QIDO-RS query.</summary>
    public static async Task<IResult> HandleSearchAsync(
        [FromServices] IDicomInstanceStore instances,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Exact study lookup (the EHR chart preview card knows the linked StudyInstanceUID). This
        // avoids relying on the DICOM-level PatientID matching the EHR patient id. Accept the tag
        // name or the DICOM tag key.
        var studyUid = request.Query["StudyInstanceUID"].FirstOrDefault()
            ?? request.Query["0020000D"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(studyUid))
        {
            var byStudy = await instances.GetByStudyAsync(studyUid, cancellationToken).ConfigureAwait(false);
            if (byStudy.Count == 0)
            {
                return Results.Json(Array.Empty<object>());
            }
            var first = byStudy[0];
            return Results.Json(new[]
            {
                BuildStudyJson(studyUid, first.PatientId, first.PatientName, first.Modality, byStudy.Count),
            });
        }

        var patientId = request.Query["PatientID"].FirstOrDefault();
        var studyDateRange = request.Query["StudyDate"].FirstOrDefault();
        var (from, to) = ParseDateRange(studyDateRange);

        var studies = await instances.SearchStudiesAsync(patientId, from, to, cancellationToken).ConfigureAwait(false);

        return Results.Json(studies
            .Select(s => BuildStudyJson(
                s.StudyInstanceUid, s.PatientId, s.PatientName, modality: null, s.Series.Sum(se => se.Instances.Count)))
            .ToArray());
    }

    // DICOM-JSON shape: tag-keyed objects. The minimum a QIDO-RS client expects per study is
    // 0020,000D (StudyInstanceUID) and 0010,0020 (PatientID); we also surface the modality
    // (0008,0060) and number of study-related instances (0020,1208) for the chart preview card.
    private static Dictionary<string, object?> BuildStudyJson(
        string studyInstanceUid, string? patientId, string? patientName, string? modality, int instanceCount)
    {
        var json = new Dictionary<string, object?>
        {
            ["0020000D"] = new { vr = "UI", Value = new[] { studyInstanceUid } },
            ["00100010"] = new { vr = "PN", Value = new[] { new { Alphabetic = patientName ?? "" } } },
            ["00100020"] = new { vr = "LO", Value = new[] { patientId ?? "" } },
            ["00080050"] = new { vr = "SH", Value = Array.Empty<string>() }, // AccessionNumber
            ["00201208"] = new { vr = "IS", Value = new[] { instanceCount } },
        };
        if (!string.IsNullOrWhiteSpace(modality))
        {
            json["00080060"] = new { vr = "CS", Value = new[] { modality } };
        }
        return json;
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
            s, "yyyyMMdd", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var dt) ? dt : null;
}
