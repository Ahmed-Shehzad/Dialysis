using System.Text.Json;
using Dialysis.Hie.Core.Abstraction.OpenEhr;
using Hl7.Fhir.Model;

namespace Dialysis.Hie.OpenEhr.Archetypes;

/// <summary>
/// Projects FHIR <see cref="Patient"/> to a flattened openEHR-DEMOGRAPHIC-PERSON.person.v1 shape.
/// The JSON keys follow the archetype's path notation so EHRbase ingestion can map them directly.
/// </summary>
public sealed class PatientArchetypeProjection : IArchetypeProjection<Patient>
{
    public string ArchetypeId => ArchetypeIds.PatientDemographics;

    public string Project(Patient resource)
    {
        ArgumentNullException.ThrowIfNull(resource);
        var name = resource.Name.FirstOrDefault();
        var mrn = resource.Identifier.FirstOrDefault();
        var payload = new
        {
            archetype_node_id = ArchetypeIds.PatientDemographics,
            details = new
            {
                identifiers = new[]
                {
                    new { issuer = mrn?.System, value = mrn?.Value, type = mrn?.Type?.Coding.FirstOrDefault()?.Code },
                },
                name = new
                {
                    family = name?.Family,
                    given = name?.Given?.FirstOrDefault(),
                },
                birth = new
                {
                    date = resource.BirthDate,
                },
                gender = resource.Gender?.ToString(),
                links = resource.Link.Select(l => new
                {
                    type = l.Type?.ToString(),
                    other_reference = l.Other?.Reference,
                }),
            },
        };
        return JsonSerializer.Serialize(payload);
    }
}
