using Dialysis.EHR.Contracts.Integration;
using Dialysis.BuildingBlocks.Fhir.Mapping;
using Dialysis.HIE.Core.Coding;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Outbound.Mappers;

public sealed class PatientMapper :
    IFhirResourceMapper<PatientRegisteredIntegrationEvent, Patient>,
    IFhirResourceMapper<PatientDemographicsUpdatedIntegrationEvent, Patient>,
    IFhirResourceMapper<PatientsMergedIntegrationEvent, Patient>
{
    public Patient Map(PatientRegisteredIntegrationEvent e)
    {
        var patient = NewPatient(e.PatientId, e.MedicalRecordNumber);
        patient.Name.Add(new HumanName { Family = e.FamilyName, Given = [e.GivenName] });
        patient.BirthDate = e.DateOfBirth.ToString("yyyy-MM-dd");
        if (!string.IsNullOrWhiteSpace(e.SexAtBirthCode))
            patient.Gender = ParseGender(e.SexAtBirthCode);
        if (!string.IsNullOrWhiteSpace(e.PreferredLanguageCode))
        {
            patient.Communication.Add(new Patient.CommunicationComponent
            {
                Language = new CodeableConcept("urn:ietf:bcp:47", e.PreferredLanguageCode),
                Preferred = true,
            });
        }
        return patient;
    }

    public Patient Map(PatientDemographicsUpdatedIntegrationEvent e)
    {
        var patient = NewPatient(e.PatientId, e.MedicalRecordNumber);
        patient.Name.Add(new HumanName { Family = e.FamilyName, Given = [e.GivenName] });
        return patient;
    }

    public Patient Map(PatientsMergedIntegrationEvent e)
    {
        var patient = NewPatient(e.SurvivingPatientId, e.SurvivingMedicalRecordNumber);
        patient.Link.Add(new Patient.LinkComponent
        {
            Other = new ResourceReference($"Patient/{e.SupersededPatientId}"),
            Type = Patient.LinkType.Replaces,
        });
        return patient;
    }

    private static Patient NewPatient(Guid patientId, string mrn)
    {
        var patient = new Patient { Id = patientId.ToString() };
        if (!string.IsNullOrWhiteSpace(mrn))
        {
            patient.Identifier.Add(new Identifier
            {
                System = CodeSystems.MrnIdentifier,
                Value = mrn,
                Type = new CodeableConcept(CodeSystems.MrnIdentifier, "MR", "Medical record number"),
            });
        }
        return patient;
    }

    private static AdministrativeGender? ParseGender(string code) => code.ToLowerInvariant() switch
    {
        "male" or "m" => AdministrativeGender.Male,
        "female" or "f" => AdministrativeGender.Female,
        "other" or "o" => AdministrativeGender.Other,
        "unknown" or "u" => AdministrativeGender.Unknown,
        _ => null,
    };
}
