using System.Text.Json;

namespace Dialysis.Reports.Api;

/// <summary>
/// Parses FHIR R4 Bundle JSON for report aggregation. Reduces cognitive complexity.
/// </summary>
internal static class FhirBundleParser
{
    private const string ProcedureType = "Procedure";
    private const string ObservationType = "Observation";
    private const string PatientPrefix = "Patient/";

    public static IReadOnlyList<string> ExtractProcedureSessionIds(string json)
    {
        var ids = new List<string>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("entry", out var entries))
                return ids;
            foreach (var e in entries.EnumerateArray())
            {
                if (!IsResourceType(e, ProcedureType) || !TryGetId(e, "proc-", out var sessionId))
                    continue;
                ids.Add(sessionId);
            }
        }
        catch { /* ignore */ }
        return ids;
    }

    public static IReadOnlyList<PatientDurationSummary> ParseDurationByPatient(string json)
    {
        var durations = new Dictionary<string, List<double>>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("entry", out var entries))
                return [];
            foreach (var e in entries.EnumerateArray())
            {
                if (!IsResourceType(e, ProcedureType))
                    continue;
                string? mrn = ExtractPatientMrn(e);
                if (string.IsNullOrEmpty(mrn) || mrn == "unknown")
                    continue;
                double minutes = ExtractDurationMinutes(e);
                if (!durations.TryGetValue(mrn, out var list))
                {
                    list = [];
                    durations[mrn] = list;
                }
                list.Add(minutes);
            }
        }
        catch { /* ignore */ }
        return durations
            .Select(kv => new PatientDurationSummary(kv.Key, kv.Value.Count, (decimal)kv.Value.Sum(), (decimal)(kv.Value.Count > 0 ? kv.Value.Average() : 0)))
            .ToList();
    }

    public static IReadOnlyList<ObservationCountByCode> ParseObservationsByCode(string json, string? codeFilter)
    {
        var counts = new Dictionary<string, int>();
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("entry", out var entries))
                return [];
            foreach (var e in entries.EnumerateArray())
            {
                if (!IsResourceType(e, ObservationType))
                    continue;
                string code = ExtractObservationCode(e) ?? "unknown";
                if (!string.IsNullOrEmpty(codeFilter) && !code.Contains(codeFilter, StringComparison.OrdinalIgnoreCase))
                    continue;
                counts.TryGetValue(code, out int count);
                counts[code] = count + 1;
            }
        }
        catch { /* ignore */ }
        return counts.OrderByDescending(kv => kv.Value).Select(kv => new ObservationCountByCode(kv.Key, kv.Value)).ToList();
    }

    private static bool IsResourceType(JsonElement entry, string resourceType)
    {
        if (!entry.TryGetProperty("resource", out var res))
            return false;
        if (!res.TryGetProperty("resourceType", out var rt))
            return false;
        return rt.GetString() == resourceType;
    }

    private static bool TryGetId(JsonElement entry, string prefix, out string id)
    {
        id = "";
        if (!entry.TryGetProperty("resource", out var res) || !res.TryGetProperty("id", out var idProp))
            return false;
        string raw = idProp.GetString() ?? "";
        if (!raw.StartsWith(prefix, StringComparison.Ordinal))
            return false;
        id = raw[prefix.Length..];
        return true;
    }

    private static string? ExtractPatientMrn(JsonElement entry)
    {
        if (!entry.TryGetProperty("resource", out var res))
            return null;
        if (!res.TryGetProperty("subject", out var sub) || !sub.TryGetProperty("reference", out var refEl))
            return null;
        string? patientRef = refEl.GetString();
        if (string.IsNullOrEmpty(patientRef) || !patientRef.StartsWith(PatientPrefix, StringComparison.Ordinal))
            return null;
        return patientRef[PatientPrefix.Length..];
    }

    private static double ExtractDurationMinutes(JsonElement entry)
    {
        if (!entry.TryGetProperty("resource", out var res) || !res.TryGetProperty("performedPeriod", out var perf))
            return 0;
        if (!perf.TryGetProperty("start", out var startEl) || !perf.TryGetProperty("end", out var endEl))
            return 0;
        if (!DateTimeOffset.TryParse(startEl.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var start) ||
            !DateTimeOffset.TryParse(endEl.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var end))
            return 0;
        return (end - start).TotalMinutes;
    }

    private static string? ExtractObservationCode(JsonElement entry)
    {
        if (!entry.TryGetProperty("resource", out var res) || !res.TryGetProperty("code", out var codeEl) || !codeEl.TryGetProperty("coding", out var codings))
            return null;
        foreach (var c in codings.EnumerateArray())
            if (c.TryGetProperty("code", out var codeProp))
                return codeProp.GetString();
        return null;
    }
}

