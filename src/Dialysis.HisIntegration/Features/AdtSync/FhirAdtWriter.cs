using Dialysis.ApiClients;
using Dialysis.HisIntegration.Services;
using Hl7.Fhir.Model;

namespace Dialysis.HisIntegration.Features.AdtSync;

public sealed class FhirAdtWriterOptions
{
    public const string SectionName = "Fhir";
    public string? BaseUrl { get; set; }
    public Dictionary<string, string>? TenantBaseUrls { get; set; }
}

public sealed class FhirAdtWriter : IFhirAdtWriter
{
    private readonly IFhirApiFactory _fhirApiFactory;
    private readonly ITenantFhirResolver _resolver;

    public FhirAdtWriter(IFhirApiFactory fhirApiFactory, ITenantFhirResolver resolver)
    {
        _fhirApiFactory = fhirApiFactory;
        _resolver = resolver;
    }

    public async Task<(string? PatientId, string? EncounterId)> WriteAdtAsync(AdtParsedData data, CancellationToken cancellationToken = default)
    {
        var baseUrl = _resolver.GetBaseUrl(null);
        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(data.Mrn))
            return (null, null);

        var api = _fhirApiFactory.ForBaseUrl(baseUrl);
        var patient = MapToPatient(data);
        var patientRes = await api.CreatePatient(patient, cancellationToken);
        patientRes.EnsureSuccessStatusCode();
        var patientId = ExtractIdFromLocation(patientRes.Headers.Location?.ToString());

        var encounter = MapToEncounter(data, patientId);
        var encounterRes = await api.CreateEncounter(encounter, cancellationToken);
        encounterRes.EnsureSuccessStatusCode();
        var encounterId = ExtractIdFromLocation(encounterRes.Headers.Location?.ToString());

        return (patientId, encounterId);
    }

    private static Patient MapToPatient(AdtParsedData data)
    {
        var patient = new Patient
        {
            Identifier =
            [
                new Identifier
                {
                    System = "http://hospital.example.org/mrn",
                    Value = data.Mrn,
                    Use = Identifier.IdentifierUse.Official
                }
            ],
            Name =
            [
                new HumanName
                {
                    Family = data.FamilyName,
                    Given = data.GivenName != null ? [data.GivenName] : [],
                    Use = HumanName.NameUse.Official
                }
            ]
        };
        if (!string.IsNullOrEmpty(data.BirthDate) && DateTime.TryParse(data.BirthDate, out var dob))
        {
            patient.BirthDate = dob.ToString("yyyy-MM-dd");
        }
        if (!string.IsNullOrEmpty(data.Gender))
        {
            patient.Gender = data.Gender?.ToLowerInvariant() switch
            {
                "m" or "male" => AdministrativeGender.Male,
                "f" or "female" => AdministrativeGender.Female,
                "o" or "other" => AdministrativeGender.Other,
                _ => AdministrativeGender.Unknown
            };
        }
        return patient;
    }

    private static Encounter MapToEncounter(AdtParsedData data, string? patientId)
    {
        var encounter = new Encounter
        {
            Status = Encounter.EncounterStatus.InProgress,
            Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "IMP", "inpatient encounter")
        };
        if (!string.IsNullOrEmpty(patientId))
        {
            encounter.Subject = new ResourceReference($"Patient/{patientId}");
        }
        if (!string.IsNullOrEmpty(data.AdmitDateTime) && DateTime.TryParse(data.AdmitDateTime, out var periodStart))
        {
            encounter.Period = new Period
            {
                Start = periodStart.ToString("yyyy-MM-ddTHH:mm:sszzz")
            };
        }
        if (!string.IsNullOrEmpty(data.DischargeDateTime) && DateTime.TryParse(data.DischargeDateTime, out var periodEnd) && encounter.Period != null)
        {
            encounter.Period.End = periodEnd.ToString("yyyy-MM-ddTHH:mm:sszzz");
        }
        if (!string.IsNullOrEmpty(data.Ward))
        {
            encounter.Location =
            [
                new Encounter.LocationComponent
                {
                    Location = new ResourceReference($"Location/{data.Ward}")
                }
            ];
        }
        return encounter;
    }

    private static string? ExtractIdFromLocation(string? location)
    {
        if (string.IsNullOrEmpty(location))
        {
            return null;
        }
        var segments = location.TrimEnd('/').Split('/');
        return segments.Length >= 2 ? segments[^1] : null;
    }
}
