namespace Dialysis.BuildingBlocks.Fhir.Validation.Authoring;

/// <summary>
/// One element-level constraint applied on top of a base resource when authoring a profile.
/// SDK-free DTO so the spec can be POSTed as JSON and bound by the on-demand authoring endpoint.
/// </summary>
public sealed record FhirElementConstraint
{
    /// <summary>FHIR element path, e.g. <c>Patient.identifier</c> or <c>Observation.value[x]</c>.</summary>
    public required string Path { get; init; }

    /// <summary>Minimum cardinality override (e.g. 1 to make an element mandatory).</summary>
    public int? Min { get; init; }

    /// <summary>Maximum cardinality override (e.g. <c>1</c> or <c>*</c>).</summary>
    public string? Max { get; init; }

    /// <summary>US-Core-style must-support flag.</summary>
    public bool? MustSupport { get; init; }

    public string? Short { get; init; }

    public string? Definition { get; init; }

    /// <summary>Restrict the element to a single type code (e.g. <c>string</c>, <c>CodeableConcept</c>).</summary>
    public string? TypeCode { get; init; }

    /// <summary>Bind the element to a value set canonical (with <see cref="BindingStrength"/>).</summary>
    public string? BindingValueSet { get; init; }

    /// <summary><c>required</c> | <c>extensible</c> | <c>preferred</c> | <c>example</c>.</summary>
    public string? BindingStrength { get; init; }

    public string? FixedString { get; init; }

    public string? FixedCode { get; init; }

    public string? FixedUri { get; init; }
}

/// <summary>
/// Declarative request to author a constraint <c>StructureDefinition</c> on the fly. The factory
/// turns this into a differential, runs the Firely snapshot generator to prove it is computable,
/// and the verifier confirms correctness before it is published.
/// </summary>
public sealed record FhirProfileSpec
{
    /// <summary>Base resource being profiled, e.g. <c>Patient</c>.</summary>
    public required string BaseResourceType { get; init; }

    /// <summary>Canonical URL of the new profile.</summary>
    public required string Url { get; init; }

    /// <summary>Computer-friendly name (also used as the resource id).</summary>
    public required string Name { get; init; }

    public string? Title { get; init; }

    public string? Version { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// Base definition to derive from. Defaults to the core
    /// <c>http://hl7.org/fhir/StructureDefinition/{BaseResourceType}</c> when omitted, but may point
    /// at another authored profile (resolved via the conformance registry) to layer profiles.
    /// </summary>
    public string? BaseDefinition { get; init; }

    public IReadOnlyList<FhirElementConstraint> Constraints { get; init; } = [];

    /// <summary>Fluent builder for code-first authoring (tests, composition-time catalogues).</summary>
    public static FhirProfileSpecBuilder For(string baseResourceType, string url, string name)
        => new(baseResourceType, url, name);
}

/// <summary>Fluent builder producing an immutable <see cref="FhirProfileSpec"/>.</summary>
public sealed class FhirProfileSpecBuilder
{
    private readonly List<FhirElementConstraint> _constraints = [];
    private string? _title;
    private string? _version;
    private string? _description;
    private string? _baseDefinition;
    private readonly string _baseResourceType;
    private readonly string _url;
    private readonly string _name;
    /// <summary>Fluent builder producing an immutable <see cref="FhirProfileSpec"/>.</summary>
    public FhirProfileSpecBuilder(string baseResourceType, string url, string name)
    {
        _baseResourceType = baseResourceType;
        _url = url;
        _name = name;
    }

    public FhirProfileSpecBuilder Title(string title) { _title = title; return this; }

    public FhirProfileSpecBuilder Version(string version) { _version = version; return this; }

    public FhirProfileSpecBuilder Description(string description) { _description = description; return this; }

    public FhirProfileSpecBuilder DerivedFrom(string baseDefinition) { _baseDefinition = baseDefinition; return this; }

    public FhirProfileSpecBuilder Constrain(FhirElementConstraint constraint)
    {
        _constraints.Add(constraint);
        return this;
    }

    public FhirProfileSpecBuilder Require(string path, bool mustSupport = true)
        => Constrain(new FhirElementConstraint { Path = path, Min = 1, MustSupport = mustSupport });

    public FhirProfileSpecBuilder Forbid(string path)
        => Constrain(new FhirElementConstraint { Path = path, Max = "0" });

    public FhirProfileSpecBuilder MustSupport(string path)
        => Constrain(new FhirElementConstraint { Path = path, MustSupport = true });

    public FhirProfileSpecBuilder Bind(string path, string valueSet, string strength = "required")
        => Constrain(new FhirElementConstraint
        {
            Path = path,
            BindingValueSet = valueSet,
            BindingStrength = strength,
        });

    public FhirProfileSpec Build() => new()
    {
        BaseResourceType = _baseResourceType,
        Url = _url,
        Name = _name,
        Title = _title,
        Version = _version,
        Description = _description,
        BaseDefinition = _baseDefinition,
        Constraints = _constraints,
    };
}

/// <summary>A single IG dependency (another package's canonical, e.g. US Core).</summary>
public sealed record FhirIgDependency
{
    public required string Uri { get; init; }

    public string? PackageId { get; init; }

    public string? Version { get; init; }
}

/// <summary>
/// Declarative request to author an <c>ImplementationGuide</c> on the fly. Each contained profile
/// spec is built + verified; the IG bundles them with package metadata, dependencies and
/// per-type global profiles.
/// </summary>
public sealed record FhirImplementationGuideSpec
{
    /// <summary>NPM-style package id, e.g. <c>dialysis.fhir.core</c>.</summary>
    public required string PackageId { get; init; }

    public required string Url { get; init; }

    public required string Name { get; init; }

    public string? Title { get; init; }

    public string Version { get; init; } = "0.1.0";

    public IReadOnlyList<FhirProfileSpec> Profiles { get; init; } = [];

    public IReadOnlyList<FhirIgDependency> DependsOn { get; init; } = [];
}
