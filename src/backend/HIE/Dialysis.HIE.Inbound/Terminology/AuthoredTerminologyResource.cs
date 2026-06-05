namespace Dialysis.HIE.Inbound.Terminology;

/// <summary>
/// A governed, operator-authored canonical terminology resource (CodeSystem / ValueSet / ConceptMap)
/// persisted as FHIR JSON with a canonical url + version + lifecycle status. The authoring admin
/// surface drives create/revise/status; the <c>TerminologyCatalogLoader</c> overlays every
/// <c>active</c> row onto the in-memory <c>DialysisTerminologyCatalog</c> at host startup so the
/// authored resources serve via <c>$validate-code</c> / <c>$expand</c> / <c>$translate</c> alongside
/// the built-ins. Versioning is deliberate (a new version is a new row), never hot-reloaded.
/// </summary>
public sealed class AuthoredTerminologyResource
{
    /// <summary>The canonical FHIR resource types that may be authored here.</summary>
    public static readonly IReadOnlySet<string> AllowedResourceTypes =
        new HashSet<string>(StringComparer.Ordinal) { "CodeSystem", "ValueSet", "ConceptMap" };

    /// <summary>Lifecycle states; only <c>active</c> resources are loaded into the catalog.</summary>
    public static readonly IReadOnlySet<string> AllowedStatuses =
        new HashSet<string>(StringComparer.Ordinal) { "draft", "active", "retired" };

    public Guid Id { get; private set; }
    public string ResourceType { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public string Status { get; private set; } = "draft";
    public string Name { get; private set; } = string.Empty;
    public string FhirJson { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public string UpdatedBy { get; private set; } = string.Empty;

    private AuthoredTerminologyResource() { }

    /// <summary>Authors a new terminology resource version.</summary>
    public AuthoredTerminologyResource(
        Guid id, string resourceType, string url, string version, string status,
        string name, string fhirJson, DateTime now, string updatedBy)
    {
        ValidateResourceType(resourceType);
        ValidateStatus(status);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(fhirJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        Id = id == Guid.Empty ? Guid.CreateVersion7() : id;
        ResourceType = resourceType;
        Url = url.Trim();
        Version = version.Trim();
        Status = status;
        Name = name.Trim();
        FhirJson = fhirJson;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
        UpdatedBy = updatedBy.Trim();
    }

    /// <summary>Replaces the authored body/metadata for this (url, version) row.</summary>
    public void Revise(string status, string name, string fhirJson, DateTime now, string updatedBy)
    {
        ValidateStatus(status);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(fhirJson);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);

        Status = status;
        Name = name.Trim();
        FhirJson = fhirJson;
        UpdatedAtUtc = now;
        UpdatedBy = updatedBy.Trim();
    }

    /// <summary>Transitions the lifecycle status (e.g. draft → active, active → retired).</summary>
    public void SetStatus(string status, DateTime now, string updatedBy)
    {
        ValidateStatus(status);
        ArgumentException.ThrowIfNullOrWhiteSpace(updatedBy);
        Status = status;
        UpdatedAtUtc = now;
        UpdatedBy = updatedBy.Trim();
    }

    private static void ValidateResourceType(string resourceType)
    {
        if (!AllowedResourceTypes.Contains(resourceType))
            throw new ArgumentException(
                $"Resource type '{resourceType}' is not authorable; expected one of {string.Join(", ", AllowedResourceTypes)}.",
                nameof(resourceType));
    }

    private static void ValidateStatus(string status)
    {
        if (!AllowedStatuses.Contains(status))
            throw new ArgumentException(
                $"Status '{status}' is invalid; expected one of {string.Join(", ", AllowedStatuses)}.",
                nameof(status));
    }
}
