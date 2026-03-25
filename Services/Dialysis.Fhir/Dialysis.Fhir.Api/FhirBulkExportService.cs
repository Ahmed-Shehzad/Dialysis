using BuildingBlocks.Tenancy;

using Dialysis.Hl7ToFhir;

using Hl7.Fhir.Model;

using Microsoft.Extensions.Primitives;

namespace Dialysis.Fhir.Api;

/// <summary>
/// Aggregates FHIR resources from Patient, Device, Prescription, Treatment, and Alarm services.
/// </summary>
public sealed class FhirBulkExportService
{
    private readonly IFhirExportGatewayApi _api;
    private readonly ITenantContext _tenant;

    private readonly record struct GatewayHttpContext(
        string? Auth,
        string? TenantId,
        CancellationToken CancellationToken);

    private static readonly IReadOnlyDictionary<string, string> TypeToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Patient"] = "api/patients/fhir",
        ["Device"] = "api/devices/fhir",
        ["ServiceRequest"] = "api/prescriptions/fhir",
        ["Procedure"] = "api/treatment-sessions/fhir",
        ["Observation"] = "api/treatment-sessions/fhir",
        ["Provenance"] = "api/treatment-sessions/fhir",
        ["DetectedIssue"] = "api/alarms/fhir",
        ["AuditEvent"] = "api/audit-events",
    };

    public FhirBulkExportService(IFhirExportGatewayApi api, ITenantContext tenant)
    {
        _api = api;
        _tenant = tenant;
    }

    /// <summary>
    /// FHIR-type search - returns a search set Bundle for the given resource type.
    /// Supports Patient, Device, ServiceRequest, Procedure, Observation, DetectedIssue, AuditEvent.
    /// </summary>
    public async Task<Bundle> SearchAsync(
        string resourceType,
        IReadOnlyDictionary<string, string?> searchParams,
        HttpRequest? originalRequest,
        CancellationToken cancellationToken = default)
    {
        if (!TypeToPath.TryGetValue(resourceType, out string? path))
            throw new ArgumentException($"Unsupported search resource type: {resourceType}", nameof(resourceType));

        string? auth = null;
        if (originalRequest != null)
        {
            StringValues authHeader = originalRequest.Headers.Authorization;
            if (!StringValues.IsNullOrEmpty(authHeader))
                auth = authHeader.ToString();
        }
        string? tenantId = string.IsNullOrEmpty(_tenant.TenantId) ? null : _tenant.TenantId;
        int limit = 100;
        if (searchParams.TryGetValue("_count", out string? countStr) && int.TryParse(countStr, out int c))
            limit = Math.Min(Math.Max(1, c), 1000);

        var http = new GatewayHttpContext(auth, tenantId, cancellationToken);

        Bundle? bundle = resourceType switch
        {
            "Patient" => await FetchPatientSearchAsync(
                limit,
                GetParam(searchParams, "_id"),
                GetParam(searchParams, "identifier"),
                http),
            "Device" => await FetchDeviceSearchAsync(
                GetParam(searchParams, "_id"),
                GetParam(searchParams, "identifier"),
                http),
            "ServiceRequest" => await FetchPrescriptionSearchAsync(
                limit,
                ExtractMrn(GetParam(searchParams, "subject") ?? GetParam(searchParams, "patient")),
                http),
            "Procedure" or "Observation" or "Provenance" => await FetchTreatmentSearchAsync(
                limit,
                ExtractMrn(GetParam(searchParams, "subject") ?? GetParam(searchParams, "patient")),
                GetParam(searchParams, "date"),
                GetParam(searchParams, "dateFrom"),
                GetParam(searchParams, "dateTo"),
                http),
            "DetectedIssue" => await FetchAlarmSearchAsync(
                limit,
                GetParam(searchParams, "_id"),
                GetParam(searchParams, "device"),
                GetParam(searchParams, "date"),
                GetParam(searchParams, "from"),
                GetParam(searchParams, "to"),
                http),
            "AuditEvent" => await FetchAuditEventSearchAsync(
                limit,
                http),
            _ => await FetchBundleByPathAsync(path, limit, null, null, http),
        };

        if (bundle is null)
            return new Bundle { Type = Bundle.BundleType.Searchset, Entry = [] };

        bundle.Type = Bundle.BundleType.Searchset;
        return bundle;
    }

    private static string? GetParam(IReadOnlyDictionary<string, string?> searchParams, string key) =>
        searchParams.TryGetValue(key, out string? v) ? v : null;

    /// <summary>
    /// Extracts MRN from FHIR reference (e.g. "Patient/MRN123" -> "MRN123").
    /// </summary>
    private static string? ExtractMrn(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;
        if (reference.StartsWith("Patient/", StringComparison.OrdinalIgnoreCase))
            return reference["Patient/".Length..].Trim();
        return reference.Trim();
    }

    private async Task<Bundle?> FetchPatientSearchAsync(
        int limit,
        string? id,
        string? identifier,
        GatewayHttpContext http)
    {
        HttpResponseMessage response = await _api.GetPatientsFhirAsync(limit, id, identifier, http.Auth, http.TenantId, http.CancellationToken);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return null;
            string json = await response.Content.ReadAsStringAsync(http.CancellationToken);
            return FhirJsonHelper.FromJson<Bundle>(json);
        }
    }

    private async Task<Bundle?> FetchDeviceSearchAsync(
        string? id,
        string? identifier,
        GatewayHttpContext http)
    {
        HttpResponseMessage response = await _api.GetDevicesFhirAsync(id, identifier, http.Auth, http.TenantId, http.CancellationToken);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return null;
            string json = await response.Content.ReadAsStringAsync(http.CancellationToken);
            return FhirJsonHelper.FromJson<Bundle>(json);
        }
    }

    private async Task<Bundle?> FetchPrescriptionSearchAsync(
        int limit,
        string? mrn,
        GatewayHttpContext http)
    {
        HttpResponseMessage response = await _api.GetPrescriptionsFhirAsync(limit, mrn, mrn, http.Auth, http.TenantId, http.CancellationToken);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return null;
            string json = await response.Content.ReadAsStringAsync(http.CancellationToken);
            return FhirJsonHelper.FromJson<Bundle>(json);
        }
    }

    private async Task<Bundle?> FetchTreatmentSearchAsync(
        int limit,
        string? mrn,
        string? date,
        string? dateFrom,
        string? dateTo,
        GatewayHttpContext http)
    {
        HttpResponseMessage response = await _api.GetTreatmentSessionsFhirAsync(
            new TreatmentSessionsFhirQuery(limit, mrn, mrn, date, dateFrom, dateTo), http.Auth, http.TenantId, http.CancellationToken);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return null;
            string json = await response.Content.ReadAsStringAsync(http.CancellationToken);
            return FhirJsonHelper.FromJson<Bundle>(json);
        }
    }

    private async Task<Bundle?> FetchAlarmSearchAsync(
        int limit,
        string? id,
        string? deviceRef,
        string? date,
        string? from,
        string? to,
        GatewayHttpContext http)
    {
        string? deviceId = ExtractReferenceId(deviceRef, "Device");
        HttpResponseMessage response = await _api.GetAlarmsFhirAsync(
            new AlarmsFhirQuery(limit, id, deviceId, null, date, from, to), http.Auth, http.TenantId, http.CancellationToken);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return null;
            string json = await response.Content.ReadAsStringAsync(http.CancellationToken);
            return FhirJsonHelper.FromJson<Bundle>(json);
        }
    }

    private async Task<Bundle?> FetchAuditEventSearchAsync(
        int limit,
        GatewayHttpContext http)
    {
        HttpResponseMessage response = await _api.GetAuditEventsAsync(
            Math.Min(limit, 500), http.Auth, http.TenantId, http.CancellationToken);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return null;
            string json = await response.Content.ReadAsStringAsync(http.CancellationToken);
            return FhirJsonHelper.FromJson<Bundle>(json);
        }
    }

    private static string? ExtractReferenceId(string? reference, string prefix)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return null;
        string p = prefix + "/";
        if (reference.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            return reference[p.Length..].Trim();
        return reference.Trim();
    }

    private async Task<Bundle?> FetchBundleByPathAsync(
        string path,
        int limit,
        string? patientId,
        string? since,
        GatewayHttpContext http)
    {
        HttpResponseMessage response = path switch
        {
            _ when path.Contains("patients", StringComparison.OrdinalIgnoreCase) => await _api.GetPatientsFhirAsync(
                limit, null, null, http.Auth, http.TenantId, http.CancellationToken),
            _ when path.Contains("devices", StringComparison.OrdinalIgnoreCase) => await _api.GetDevicesFhirAsync(
                null, null, http.Auth, http.TenantId, http.CancellationToken),
            _ when path.Contains("prescriptions", StringComparison.OrdinalIgnoreCase) => await _api.GetPrescriptionsFhirAsync(
                limit, patientId, patientId, http.Auth, http.TenantId, http.CancellationToken),
            _ when path.Contains("treatment-sessions", StringComparison.OrdinalIgnoreCase) => await _api.GetTreatmentSessionsFhirAsync(
                new TreatmentSessionsFhirQuery(limit, patientId, patientId, null, since, null), http.Auth, http.TenantId, http.CancellationToken),
            _ when path.Contains("alarms", StringComparison.OrdinalIgnoreCase) => await _api.GetAlarmsFhirAsync(
                new AlarmsFhirQuery(limit, null, null, null, null, since, null), http.Auth, http.TenantId, http.CancellationToken),
            _ when path.Contains("audit-events", StringComparison.OrdinalIgnoreCase) => await _api.GetAuditEventsAsync(
                Math.Min(limit, 500), http.Auth, http.TenantId, http.CancellationToken),
            _ => throw new InvalidOperationException($"Unknown path: {path}"),
        };

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return null;
            string json = await response.Content.ReadAsStringAsync(http.CancellationToken);
            return FhirJsonHelper.FromJson<Bundle>(json);
        }
    }

    public async Task<Bundle> ExportAsync(
        string[] types,
        int limitPerType,
        string? patientId,
        DateTimeOffset? since,
        HttpRequest? originalRequest,
        CancellationToken cancellationToken = default)
    {
        HashSet<string> requestedTypes = NormalizeRequestedTypes(types);
        HashSet<string> pathsToFetch = ResolvePaths(requestedTypes);
        var bundle = new Bundle { Type = Bundle.BundleType.Collection, Entry = [] };

        string? auth = null;
        if (originalRequest != null)
        {
            StringValues authHeader = originalRequest.Headers.Authorization;
            if (!StringValues.IsNullOrEmpty(authHeader))
                auth = authHeader.ToString();
        }
        string? tenantId = string.IsNullOrEmpty(_tenant.TenantId) ? null : _tenant.TenantId;

        var exportHttp = new GatewayHttpContext(auth, tenantId, cancellationToken);

        foreach (string path in pathsToFetch)
        {
            Bundle? sourceBundle = await FetchBundleAsync(path, limitPerType, patientId, since, exportHttp);
            MergeEntries(bundle, sourceBundle, requestedTypes);
        }

        return bundle;
    }

    private static HashSet<string> NormalizeRequestedTypes(string[] types)
    {
        var requestedTypes = new HashSet<string>(types.Select(t => t.Trim()), StringComparer.OrdinalIgnoreCase);
        if (requestedTypes.Count == 0)
            _ = requestedTypes.Add("Patient");
        return requestedTypes;
    }

    private static HashSet<string> ResolvePaths(HashSet<string> requestedTypes)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string type in requestedTypes)
            if (TypeToPath.TryGetValue(type, out string? path))
                _ = paths.Add(path);

        return paths;
    }

    private async Task<Bundle?> FetchBundleAsync(
        string path,
        int limit,
        string? patientId,
        DateTimeOffset? since,
        GatewayHttpContext http)
    {
        string? sinceStr = since?.ToString("o");
        HttpResponseMessage response = path switch
        {
            _ when path.Contains("patients", StringComparison.OrdinalIgnoreCase) => await _api.GetPatientsFhirAsync(
                limit, null, patientId, http.Auth, http.TenantId, http.CancellationToken),
            _ when path.Contains("devices", StringComparison.OrdinalIgnoreCase) => await _api.GetDevicesFhirAsync(
                null, null, http.Auth, http.TenantId, http.CancellationToken),
            _ when path.Contains("prescriptions", StringComparison.OrdinalIgnoreCase) => await _api.GetPrescriptionsFhirAsync(
                limit, patientId, patientId, http.Auth, http.TenantId, http.CancellationToken),
            _ when path.Contains("treatment-sessions", StringComparison.OrdinalIgnoreCase) => await _api.GetTreatmentSessionsFhirAsync(
                new TreatmentSessionsFhirQuery(limit, patientId, patientId, null, sinceStr, null), http.Auth, http.TenantId, http.CancellationToken),
            _ when path.Contains("alarms", StringComparison.OrdinalIgnoreCase) => await _api.GetAlarmsFhirAsync(
                new AlarmsFhirQuery(limit, null, null, null, null, sinceStr, null), http.Auth, http.TenantId, http.CancellationToken),
            _ when path.Contains("audit-events", StringComparison.OrdinalIgnoreCase) => await _api.GetAuditEventsAsync(
                Math.Min(limit, 500), http.Auth, http.TenantId, http.CancellationToken),
            _ => throw new InvalidOperationException($"Unknown path: {path}"),
        };

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return null;
            string json = await response.Content.ReadAsStringAsync(http.CancellationToken);
            return FhirJsonHelper.FromJson<Bundle>(json);
        }
    }

    private static void MergeEntries(Bundle target, Bundle? source, HashSet<string> requestedTypes)
    {
        if (source?.Entry is null)
            return;
        foreach (Bundle.EntryComponent entry in source.Entry)
        {
            if (entry.Resource is null || !requestedTypes.Contains(entry.Resource.TypeName))
                continue;
            target.Entry.Add(entry);
        }
    }
}
