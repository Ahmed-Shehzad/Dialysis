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
    private readonly HttpClient _httpClient;
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

    public FhirBulkExportService(HttpClient httpClient, ITenantContext tenant)
    {
        _httpClient = httpClient;
        _tenant = tenant;
    }

    public async Task<Bundle> ExportAsync(string[] types, int limitPerType, Microsoft.AspNetCore.Http.HttpRequest? originalRequest, CancellationToken cancellationToken = default)
    {
        HashSet<string> requestedTypes = NormalizeRequestedTypes(types);
        HashSet<string> pathsToFetch = ResolvePaths(requestedTypes);
        var bundle = new Bundle { Type = Bundle.BundleType.Collection, Entry = [] };

        foreach (string path in pathsToFetch)
        {
            Bundle? sourceBundle = await FetchBundleAsync(path, limitPerType, originalRequest, cancellationToken);
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

    private async Task<Bundle?> FetchBundleAsync(string path, int limit, Microsoft.AspNetCore.Http.HttpRequest? originalRequest, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path + "?limit=" + limit);
        StringValues authHeader = originalRequest?.Headers["Authorization"] ?? default;
        if (!StringValues.IsNullOrEmpty(authHeader))
            _ = request.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
        if (!string.IsNullOrEmpty(_tenant.TenantId))
            _ = request.Headers.TryAddWithoutValidation("X-Tenant-Id", _tenant.TenantId);

        HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;
        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        return FhirJsonHelper.FromJson<Bundle>(json);
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
