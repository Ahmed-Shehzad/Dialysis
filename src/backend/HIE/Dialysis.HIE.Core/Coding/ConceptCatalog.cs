using System.Collections.Concurrent;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Core.Coding;

/// <summary>
/// Thread-safe in-memory <see cref="IConceptCatalog"/>. Entries are seeded at composition time
/// (system + code + fallback display); displays may be refreshed by <see cref="ConceptCatalogValidatorHostedService"/>
/// once the upstream terminology server confirms the concept and returns the authoritative display.
/// </summary>
public sealed class ConceptCatalog : IConceptCatalog
{
    private readonly ConcurrentDictionary<string, CodeableConcept> _concepts = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConceptCatalogEntry> _entries = new(StringComparer.Ordinal);

    public ConceptCatalog(IEnumerable<ConceptCatalogEntry> entries)
    {
        foreach (var entry in entries)
        {
            _entries[entry.Name] = entry;
            _concepts[entry.Name] = BuildCodeableConcept(entry, entry.FallbackDisplay);
        }
    }

    public CodeableConcept Get(string conceptName) =>
        TryGet(conceptName) ?? throw new KeyNotFoundException(
            $"Concept '{conceptName}' is not registered. Add a ConceptCatalogEntry in module composition.");

    public CodeableConcept? TryGet(string conceptName) =>
        _concepts.TryGetValue(conceptName, out var concept) ? (CodeableConcept)concept.DeepCopy() : null;

    /// <summary>Returns the registered raw entries — used by the startup validator.</summary>
    public IReadOnlyCollection<ConceptCatalogEntry> Entries => [.. _entries.Values];

    /// <summary>Updates the display value for an existing concept (called by the validator after $lookup).</summary>
    public void UpdateDisplay(string conceptName, string display)
    {
        if (!_entries.TryGetValue(conceptName, out var entry))
            return;
        _concepts[conceptName] = BuildCodeableConcept(entry, display);
    }

    private static CodeableConcept BuildCodeableConcept(ConceptCatalogEntry entry, string display) =>
        new()
        {
            Coding = [new Hl7.Fhir.Model.Coding(entry.System, entry.Code, display)],
            Text = display,
        };
}
