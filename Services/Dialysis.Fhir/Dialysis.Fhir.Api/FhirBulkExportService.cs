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
    /// Supports Patient (_id, identifier).
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
        if (originalRequest is { } req)
        {
            Microsoft.Extensions.Primitives.StringValues authHeader = req.Headers["Authorization"];
            if (!Microsoft.Extensions.Primitives.StringValues.IsNullOrEmpty(authHeader))
                auth = authHeader.ToString();
        }
        string? tenantId = string.IsNullOrEmpty(_tenant.TenantId) ? null : _tenant.TenantId;
        int limit = 100;
        if (searchParams.TryGetValue("_count", out string? countStr) && int.TryParse(countStr, out int c))
            limit = Math.Min(Math.Max(1, c), 1000);

        Bundle? bundle = resourceType switch
        {
            "Patient" => await FetchPatientSearchAsync(
                limit,
                searchParams.TryGetValue("_id", out string? id) ? id : null,
                searchParams.TryGetValue("identifier", out string? ident) ? ident : null,
                auth,
                tenantId,
                cancellationToken),
            _ => await FetchBundleByPathAsync(path, limit, null, null, auth, tenantId, cancellationToken),
        };

        if (bundle is null)
            return new Bundle { Type = Bundle.BundleType.Searchset, Entry = [] };

        bundle.Type = Bundle.BundleType.Searchset;
        return bundle;
    }

    private async Task<Bundle?> FetchPatientSearchAsync(
        int limit,
        string? id,
        string? identifier,
        string? auth,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = await _api.GetPatientsFhirAsync(limit, id, identifier, auth, tenantId, cancellationToken);
        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return null;
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            return FhirJsonHelper.FromJson<Bundle>(json);
        }
    }

    private async Task<Bundle?> FetchBundleByPathAsync(
        string path,
        int limit,
        string? patientId,
        string? since,
        string? auth,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = path switch
        {
            _ when path.Contains("patients", StringComparison.OrdinalIgnoreCase) => await _api.GetPatientsFhirAsync(
                limit, null, null, auth, tenantId, cancellationToken),
            _ when path.Contains("devices", StringComparison.OrdinalIgnoreCase) => await _api.GetDevicesFhirAsync(
                limit, auth, tenantId, cancellationToken),
            _ when path.Contains("prescriptions", StringComparison.OrdinalIgnoreCase) => await _api.GetPrescriptionsFhirAsync(
                limit, patientId, patientId, auth, tenantId, cancellationToken),
            _ when path.Contains("treatment-sessions", StringComparison.OrdinalIgnoreCase) => await _api.GetTreatmentSessionsFhirAsync(
                limit, patientId, patientId, since, auth, tenantId, cancellationToken),
            _ when path.Contains("alarms", StringComparison.OrdinalIgnoreCase) => await _api.GetAlarmsFhirAsync(
                limit, since, auth, tenantId, cancellationToken),
            _ when path.Contains("audit-events", StringComparison.OrdinalIgnoreCase) => await _api.GetAuditEventsAsync(
                Math.Min(limit, 500), auth, tenantId, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown path: {path}"),
        };

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return null;
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
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
        if (originalRequest is { } req)
        {
            StringValues authHeader = req.Headers["Authorization"];
            if (!StringValues.IsNullOrEmpty(authHeader))
                auth = authHeader.ToString();
        }
        string? tenantId = string.IsNullOrEmpty(_tenant.TenantId) ? null : _tenant.TenantId;

        foreach (string path in pathsToFetch)
        {
            Bundle? sourceBundle = await FetchBundleAsync(path, limitPerType, patientId, since, auth, tenantId, cancellationToken);
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
        string? auth,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response = path switch
        {
            _ when path.Contains("patients", StringComparison.OrdinalIgnoreCase) => await _api.GetPatientsFhirAsync(
                limit, null, patientId, auth, tenantId, cancellationToken),
            _ when path.Contains("devices", StringComparison.OrdinalIgnoreCase) => await _api.GetDevicesFhirAsync(
                limit, auth, tenantId, cancellationToken),
            _ when path.Contains("prescriptions", StringComparison.OrdinalIgnoreCase) => await _api.GetPrescriptionsFhirAsync(
                limit, patientId, patientId, auth, tenantId, cancellationToken),
            _ when path.Contains("treatment-sessions", StringComparison.OrdinalIgnoreCase) => await _api.GetTreatmentSessionsFhirAsync(
                limit,
                patientId,
                patientId,
                since?.ToString("o"),
                auth,
                tenantId,
                cancellationToken),
            _ when path.Contains("alarms", StringComparison.OrdinalIgnoreCase) => await _api.GetAlarmsFhirAsync(
                limit, since?.ToString("o"), auth, tenantId, cancellationToken),
            _ when path.Contains("audit-events", StringComparison.OrdinalIgnoreCase) => await _api.GetAuditEventsAsync(
                Math.Min(limit, 500), auth, tenantId, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown path: {path}"),
        };

        using (response)
        {
            if (!response.IsSuccessStatusCode)
                return null;
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
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
