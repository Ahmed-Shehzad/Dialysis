using Hl7.Fhir.Model;
using Hl7.Fhir.Specification.Snapshot;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.BuildingBlocks.Fhir.Validation.Authoring;

/// <summary>Builds a constraint <c>StructureDefinition</c> from a <see cref="FhirProfileSpec"/>.</summary>
public interface IFhirProfileFactory
{
    /// <summary>
    /// Produces a snapshot-complete <see cref="StructureDefinition"/>. Snapshot generation runs the
    /// Firely generator against the bundled R4 core spec (+ any authored bases in the registry); a
    /// malformed differential surfaces as generator issues that the verifier then reports.
    /// </summary>
    Task<StructureDefinition> BuildAsync(FhirProfileSpec spec, CancellationToken cancellationToken);
}

/// <inheritdoc cref="IFhirProfileFactory" />
public sealed class FhirProfileFactory(IFhirConformanceRegistry registry) : IFhirProfileFactory
{
    public async Task<StructureDefinition> BuildAsync(FhirProfileSpec spec, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(spec);
        if (string.IsNullOrWhiteSpace(spec.BaseResourceType))
            throw new ArgumentException("FhirProfileSpec.BaseResourceType is required.", nameof(spec));
        if (string.IsNullOrWhiteSpace(spec.Url))
            throw new ArgumentException("FhirProfileSpec.Url is required.", nameof(spec));
        if (string.IsNullOrWhiteSpace(spec.Name))
            throw new ArgumentException("FhirProfileSpec.Name is required.", nameof(spec));

        var baseDefinition = string.IsNullOrWhiteSpace(spec.BaseDefinition)
            ? $"http://hl7.org/fhir/StructureDefinition/{spec.BaseResourceType}"
            : spec.BaseDefinition;

        var sd = new StructureDefinition
        {
            Url = spec.Url,
            Id = spec.Name,
            Name = spec.Name,
            Title = spec.Title ?? spec.Name,
            Status = PublicationStatus.Active,
            Experimental = false,
            Description = spec.Description is null ? null : new Markdown(spec.Description),
            FhirVersion = FHIRVersion.N4_0_1,
            Kind = StructureDefinition.StructureDefinitionKind.Resource,
            Abstract = false,
            Type = spec.BaseResourceType,
            BaseDefinition = baseDefinition,
            Derivation = StructureDefinition.TypeDerivationRule.Constraint,
            Version = spec.Version,
            Date = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            Differential = new StructureDefinition.DifferentialComponent(),
        };

        // Differential always opens with the resource root element.
        sd.Differential.Element.Add(new ElementDefinition
        {
            ElementId = spec.BaseResourceType,
            Path = spec.BaseResourceType,
        });

        foreach (var c in spec.Constraints)
        {
            if (string.IsNullOrWhiteSpace(c.Path))
                throw new ArgumentException("Every FhirElementConstraint requires a Path.", nameof(spec));
            if (!c.Path.StartsWith(spec.BaseResourceType + ".", StringComparison.Ordinal) &&
                !string.Equals(c.Path, spec.BaseResourceType, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Constraint path '{c.Path}' is not rooted at '{spec.BaseResourceType}'.", nameof(spec));
            }

            sd.Differential.Element.Add(BuildElement(c));
        }

        await GenerateSnapshotAsync(sd).ConfigureAwait(false);
        return sd;
    }

    private static ElementDefinition BuildElement(FhirElementConstraint c)
    {
        var element = new ElementDefinition
        {
            ElementId = c.Path,
            Path = c.Path,
            Short = c.Short,
            Definition = c.Definition is null ? null : new Markdown(c.Definition),
        };

        if (c.Min is { } min)
            element.Min = min;
        if (!string.IsNullOrWhiteSpace(c.Max))
            element.Max = c.Max;
        if (c.MustSupport is { } ms)
            element.MustSupport = ms;

        if (!string.IsNullOrWhiteSpace(c.TypeCode))
            element.Type.Add(new ElementDefinition.TypeRefComponent { Code = c.TypeCode });

        if (!string.IsNullOrWhiteSpace(c.BindingValueSet))
        {
            element.Binding = new ElementDefinition.ElementDefinitionBindingComponent
            {
                Strength = ParseStrength(c.BindingStrength),
                ValueSet = c.BindingValueSet,
            };
        }

        if (!string.IsNullOrWhiteSpace(c.FixedString))
            element.Fixed = new FhirString(c.FixedString);
        else if (!string.IsNullOrWhiteSpace(c.FixedCode))
            element.Fixed = new Code(c.FixedCode);
        else if (!string.IsNullOrWhiteSpace(c.FixedUri))
            element.Fixed = new FhirUri(c.FixedUri);

        return element;
    }

    private static BindingStrength ParseStrength(string? strength) => strength?.Trim().ToLowerInvariant() switch
    {
        "required" => Hl7.Fhir.Model.BindingStrength.Required,
        "extensible" => Hl7.Fhir.Model.BindingStrength.Extensible,
        "preferred" => Hl7.Fhir.Model.BindingStrength.Preferred,
        _ => Hl7.Fhir.Model.BindingStrength.Example,
    };

    private async Task GenerateSnapshotAsync(StructureDefinition sd)
    {
        var settings = SnapshotGeneratorSettings.CreateDefault();
        var generator = new SnapshotGenerator(registry, settings);
        await generator.UpdateAsync(sd).ConfigureAwait(false);
    }
}
