using Dialysis.ApiClients;
using Dialysis.IdentityAdmission.Features.PatientAdmission;
using Dialysis.IdentityAdmission.Features.SessionScheduling;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Options;

namespace Dialysis.IdentityAdmission.Services;

public sealed class FhirIdentityWriterOptions
{
    public const string SectionName = "Fhir";
    public string? BaseUrl { get; set; }
}

public sealed class FhirIdentityWriter : IFhirIdentityWriter
{
    private readonly IFhirApi _fhirApi;
    private readonly FhirIdentityWriterOptions _options;

    public FhirIdentityWriter(IFhirApi fhirApi, IOptions<FhirIdentityWriterOptions> options)
    {
        _fhirApi = fhirApi;
        _options = options.Value;
    }

    public async Task<(string? PatientId, string? EncounterId)> AdmitPatientAsync(AdmitPatientCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.BaseUrl) || string.IsNullOrEmpty(command.Mrn))
            return (null, null);

        var patient = MapToPatient(command);
        var patientRes = await _fhirApi.CreatePatient(patient, cancellationToken);
        patientRes.EnsureSuccessStatusCode();
        var patientId = ExtractIdFromLocation(patientRes.Headers.Location?.ToString());

        var encounter = MapToEncounter(command, patientId);
        var encounterRes = await _fhirApi.CreateEncounter(encounter, cancellationToken);
        encounterRes.EnsureSuccessStatusCode();
        var encounterId = ExtractIdFromLocation(encounterRes.Headers.Location?.ToString());

        return (patientId, encounterId);
    }

    public async Task<string?> CreateSessionAsync(CreateSessionCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_options.BaseUrl))
            return null;

        var encounter = MapToSessionEncounter(command);
        var res = await _fhirApi.CreateEncounter(encounter, cancellationToken);
        res.EnsureSuccessStatusCode();
        return ExtractIdFromLocation(res.Headers.Location?.ToString());
    }

    private static Patient MapToPatient(AdmitPatientCommand cmd)
    {
        var patient = new Patient
        {
            Identifier =
            [
                new Identifier { System = "http://hospital.example.org/mrn", Value = cmd.Mrn, Use = Identifier.IdentifierUse.Official }
            ],
            Name =
            [
                new HumanName { Family = cmd.FamilyName, Given = cmd.GivenName != null ? [cmd.GivenName] : [], Use = HumanName.NameUse.Official }
            ]
        };
        if (cmd.BirthDate.HasValue)
            patient.BirthDate = cmd.BirthDate.Value.UtcDateTime.ToString("yyyy-MM-dd");
        return patient;
    }

    private static Encounter MapToEncounter(AdmitPatientCommand cmd, string? patientId)
    {
        var encounter = new Encounter
        {
            Status = Encounter.EncounterStatus.Planned,
            Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory")
        };
        if (!string.IsNullOrEmpty(patientId))
            encounter.Subject = new ResourceReference($"Patient/{patientId}");
        if (cmd.BirthDate.HasValue)
            encounter.Period = new Period { Start = cmd.BirthDate.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ") };
        return encounter;
    }

    private static Encounter MapToSessionEncounter(CreateSessionCommand cmd)
    {
        var enc = new Encounter
        {
            Status = Encounter.EncounterStatus.Planned,
            Class = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory"),
            Subject = new ResourceReference($"Patient/{cmd.PatientId}"),
            Period = new Period { Start = cmd.ScheduledStart.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ") }
        };
        return enc;
    }

    private static string? ExtractIdFromLocation(string? location)
    {
        if (string.IsNullOrEmpty(location)) return null;
        var segments = location.TrimEnd('/').Split('/');
        return segments.Length >= 2 ? segments[^1] : null;
    }
}
