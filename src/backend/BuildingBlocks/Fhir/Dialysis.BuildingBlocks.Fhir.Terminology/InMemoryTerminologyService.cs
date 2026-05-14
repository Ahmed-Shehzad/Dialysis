using Hl7.Fhir.Model;

namespace Dialysis.BuildingBlocks.Fhir.Terminology;

/// <summary>
/// In-memory terminology service backed by registered <see cref="CodeSystem"/> + <see cref="ValueSet"/>
/// instances. Production deployments should swap to a Firely-spec-source-backed implementation that
/// loads the US Core / USCDI / CH Core packages from .tgz resources.
/// </summary>
public sealed class InMemoryTerminologyService : ITerminologyService
{
    private readonly Dictionary<string, CodeSystem> _codeSystems = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ValueSet> _valueSets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ConceptMap> _conceptMaps = new(StringComparer.Ordinal);

    public InMemoryTerminologyService Register(CodeSystem cs)
    {
        if (cs.Url is not null) _codeSystems[cs.Url] = cs;
        return this;
    }

    public InMemoryTerminologyService Register(ValueSet vs)
    {
        if (vs.Url is not null) _valueSets[vs.Url] = vs;
        return this;
    }

    public InMemoryTerminologyService Register(ConceptMap map)
    {
        if (map.Url is not null) _conceptMaps[map.Url] = map;
        return this;
    }

    public ValueTask<Parameters> LookupAsync(string system, string code, CancellationToken cancellationToken)
    {
        var parameters = new Parameters();
        if (_codeSystems.TryGetValue(system, out var cs))
        {
            var concept = cs.Concept.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.Ordinal));
            if (concept is not null)
            {
                parameters.Add("name", new FhirString(cs.Name));
                parameters.Add("display", new FhirString(concept.Display));
            }
        }
        return new ValueTask<Parameters>(parameters);
    }

    public ValueTask<Parameters> ValidateCodeAsync(string valueSetUrl, string code, string? system, CancellationToken cancellationToken)
    {
        var result = new Parameters();
        var valid = false;
        if (_valueSets.TryGetValue(valueSetUrl, out var vs))
        {
            valid = vs.Compose?.Include.Any(i =>
                (system is null || string.Equals(i.System, system, StringComparison.Ordinal)) &&
                i.Concept.Any(c => string.Equals(c.Code, code, StringComparison.Ordinal))) == true;
        }
        result.Add("result", new FhirBoolean(valid));
        return new ValueTask<Parameters>(result);
    }

    public ValueTask<Parameters> TranslateAsync(string conceptMapUrl, string sourceSystem, string sourceCode, CancellationToken cancellationToken)
    {
        var result = new Parameters();
        if (_conceptMaps.TryGetValue(conceptMapUrl, out var map))
        {
            foreach (var group in map.Group.Where(g => g.Source == sourceSystem))
            {
                foreach (var element in group.Element.Where(e => e.Code == sourceCode))
                {
                    foreach (var target in element.Target)
                    {
                        var match = new Parameters.ParameterComponent { Name = "match" };
                        match.Part.Add(new Parameters.ParameterComponent { Name = "equivalence", Value = new Code(target.Equivalence?.ToString()) });
                        match.Part.Add(new Parameters.ParameterComponent { Name = "concept", Value = new Coding(group.Target, target.Code, target.Display) });
                        result.Parameter.Add(match);
                    }
                }
            }
        }
        return new ValueTask<Parameters>(result);
    }

    public ValueTask<ValueSet> ExpandAsync(string valueSetUrl, IReadOnlyDictionary<string, string> filters, CancellationToken cancellationToken)
    {
        if (_valueSets.TryGetValue(valueSetUrl, out var vs))
        {
            var expansion = new ValueSet.ExpansionComponent { Timestamp = DateTimeOffset.UtcNow.ToString("O") };
            foreach (var include in vs.Compose?.Include ?? [])
            {
                foreach (var concept in include.Concept)
                {
                    expansion.Contains.Add(new ValueSet.ContainsComponent
                    {
                        System = include.System,
                        Code = concept.Code,
                        Display = concept.Display,
                    });
                }
            }
            var clone = (ValueSet)vs.DeepCopy();
            clone.Expansion = expansion;
            return new ValueTask<ValueSet>(clone);
        }
        return new ValueTask<ValueSet>(new ValueSet { Url = valueSetUrl });
    }
}
