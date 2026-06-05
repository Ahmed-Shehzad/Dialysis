using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Terminology;

/// <summary>Governance metadata for one canonical terminology resource the platform owns.</summary>
public sealed record CanonicalResourceSummary(string ResourceType, string Url, string? Version, string Status, string Name);

/// <summary>
/// The platform's governed terminology: the CodeSystems / ValueSets / ConceptMaps that LIS coding,
/// imaging AI findings, and cross-context validation depend on. Seeded in-memory so
/// <c>$validate-code</c> / <c>$translate</c> / <c>$expand</c> / <c>$lookup</c> answer deterministically
/// for platform codes without an upstream tx server, and so the set is inspectable for governance.
/// Each resource carries a canonical url + version + active status.
/// </summary>
public sealed class DialysisTerminologyCatalog
{
    public const string LoincSystem = "http://loinc.org";
    public const string RadLexSystem = "http://radlex.org";
    public const string DialysisLabPanelValueSet = "https://dialysis.local/fhir/ValueSet/dialysis-lab-panel";
    public const string DialysisImagingFindingsValueSet = "https://dialysis.local/fhir/ValueSet/dialysis-imaging-findings";
    public const string LocalLabToLoincConceptMap = "https://dialysis.local/fhir/ConceptMap/local-lab-to-loinc";
    public const string LocalLabSystem = "https://dialysis.local/fhir/CodeSystem/local-lab";

    private const string Version = "1.0.0";

    private readonly InMemoryTerminologyService _service;
    private readonly List<CanonicalResourceSummary> _resources;

    /// <summary>The seeded terminology service answering operations against the platform's resources.</summary>
    public ITerminologyService Service => _service;

    /// <summary>Governance listing of every canonical resource in the catalog (built-ins + authored).</summary>
    public IReadOnlyList<CanonicalResourceSummary> Resources => _resources;

    public DialysisTerminologyCatalog()
    {
        var labPanel = LabPanelValueSet();
        var imagingFindings = ImagingFindingsValueSet();
        var radlex = RadLexCodeSystem();
        var localLab = LocalLabCodeSystem();
        var labMap = LocalLabToLoinc();

        _service = new InMemoryTerminologyService()
            .Register(radlex)
            .Register(localLab)
            .Register(labPanel)
            .Register(imagingFindings)
            .Register(labMap);

        _resources =
        [
            Summary(radlex), Summary(localLab),
            Summary(labPanel), Summary(imagingFindings),
            Summary(labMap),
        ];
    }

    /// <summary>
    /// Overlays an authored canonical resource onto the catalog (registering it with the service and
    /// adding it to the governance listing). Called at host startup by the authoring store loader so
    /// DB-authored ValueSets/CodeSystems/ConceptMaps serve via <c>$validate-code</c> / <c>$expand</c> /
    /// <c>$translate</c> alongside the built-ins. A later registration for the same canonical url wins.
    /// </summary>
    public void Register(Resource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        switch (resource)
        {
            case CodeSystem cs:
                _service.Register(cs);
                ReplaceSummary(Summary(cs));
                break;
            case ValueSet vs:
                _service.Register(vs);
                ReplaceSummary(Summary(vs));
                break;
            case ConceptMap map:
                _service.Register(map);
                ReplaceSummary(Summary(map));
                break;
            default:
                throw new ArgumentException(
                    $"Only CodeSystem, ValueSet and ConceptMap can be authored; got {resource.TypeName}.",
                    nameof(resource));
        }
    }

    private void ReplaceSummary(CanonicalResourceSummary summary)
    {
        _resources.RemoveAll(r => r.ResourceType == summary.ResourceType && r.Url == summary.Url);
        _resources.Add(summary);
    }

    private static CanonicalResourceSummary Summary(CodeSystem cs) =>
        new("CodeSystem", cs.Url!, cs.Version, cs.Status?.ToString() ?? "active", cs.Name ?? cs.Url!);

    private static CanonicalResourceSummary Summary(ValueSet vs) =>
        new("ValueSet", vs.Url!, vs.Version, vs.Status?.ToString() ?? "active", vs.Name ?? vs.Url!);

    private static CanonicalResourceSummary Summary(ConceptMap map) =>
        new("ConceptMap", map.Url!, map.Version, map.Status?.ToString() ?? "active", map.Name ?? map.Url!);

    private static ValueSet LabPanelValueSet() => new()
    {
        Url = DialysisLabPanelValueSet,
        Version = Version,
        Name = "DialysisLabPanel",
        Status = PublicationStatus.Active,
        Compose = new ValueSet.ComposeComponent
        {
            Include =
            [
                new ValueSet.ConceptSetComponent
                {
                    System = LoincSystem,
                    Concept =
                    [
                        Concept("2160-0", "Creatinine [Mass/volume] in Serum or Plasma"),
                        Concept("2823-3", "Potassium [Moles/volume] in Serum or Plasma"),
                        Concept("2951-2", "Sodium [Moles/volume] in Serum or Plasma"),
                        Concept("718-7", "Hemoglobin [Mass/volume] in Blood"),
                        Concept("2276-4", "Ferritin [Mass/volume] in Serum or Plasma"),
                        Concept("2885-2", "Protein [Mass/volume] in Serum or Plasma"),
                        Concept("3094-0", "Urea nitrogen [Mass/volume] in Serum or Plasma"),
                        Concept("2777-1", "Phosphate [Mass/volume] in Serum or Plasma"),
                    ],
                },
            ],
        },
    };

    private static ValueSet ImagingFindingsValueSet() => new()
    {
        Url = DialysisImagingFindingsValueSet,
        Version = Version,
        Name = "DialysisImagingFindings",
        Status = PublicationStatus.Active,
        Compose = new ValueSet.ComposeComponent
        {
            Include =
            [
                new ValueSet.ConceptSetComponent
                {
                    System = RadLexSystem,
                    Concept =
                    [
                        Concept("RID39055", "Patent vascular access"),
                        Concept("RID35811", "Normal study"),
                        Concept("RID35825", "No acute abnormality"),
                    ],
                },
            ],
        },
    };

    private static CodeSystem RadLexCodeSystem() => new()
    {
        Url = RadLexSystem,
        Version = Version,
        Name = "RadLex",
        Status = PublicationStatus.Active,
        Concept =
        [
            CsConcept("RID39055", "Patent vascular access"),
            CsConcept("RID35811", "Normal study"),
            CsConcept("RID35825", "No acute abnormality"),
        ],
    };

    private static CodeSystem LocalLabCodeSystem() => new()
    {
        Url = LocalLabSystem,
        Version = Version,
        Name = "DialysisLocalLab",
        Status = PublicationStatus.Active,
        Concept =
        [
            CsConcept("CR", "Creatinine (local)"),
            CsConcept("K", "Potassium (local)"),
            CsConcept("HGB", "Hemoglobin (local)"),
        ],
    };

    private static ConceptMap LocalLabToLoinc() => new()
    {
        Url = LocalLabToLoincConceptMap,
        Version = Version,
        Name = "LocalLabToLoinc",
        Status = PublicationStatus.Active,
        Group =
        [
            new ConceptMap.GroupComponent
            {
                Source = LocalLabSystem,
                Target = LoincSystem,
                Element =
                [
                    MapElement("CR", "2160-0", "Creatinine [Mass/volume] in Serum or Plasma"),
                    MapElement("K", "2823-3", "Potassium [Moles/volume] in Serum or Plasma"),
                    MapElement("HGB", "718-7", "Hemoglobin [Mass/volume] in Blood"),
                ],
            },
        ],
    };

    private static ValueSet.ConceptReferenceComponent Concept(string code, string display) =>
        new() { Code = code, Display = display };

    private static CodeSystem.ConceptDefinitionComponent CsConcept(string code, string display) =>
        new() { Code = code, Display = display };

    private static ConceptMap.SourceElementComponent MapElement(string source, string targetCode, string targetDisplay) =>
        new()
        {
            Code = source,
            Target =
            [
                new ConceptMap.TargetElementComponent
                {
                    Code = targetCode,
                    Display = targetDisplay,
                    Equivalence = ConceptMapEquivalence.Equivalent,
                },
            ],
        };
}
