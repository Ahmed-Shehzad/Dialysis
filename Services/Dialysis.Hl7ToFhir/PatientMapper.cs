using Hl7.Fhir.Model;

namespace Dialysis.Hl7ToFhir;

/// <summary>
/// Maps PDQ patient demographics (PID segment) to FHIR Patient resource.
/// MRN maps to Patient.identifier; PID-5 maps to Patient.name; PID-8 maps to Patient.gender.
/// </summary>
public static class PatientMapper
{
    private const string MrnSystem = "urn:oid:2.16.840.1.113883.19.5";

    public static Patient ToFhirPatient(PatientMappingInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var patient = new Patient
        {
            Identifier =
            [
                new Identifier(MrnSystem, input.Mrn) { Type = new CodeableConcept("http://terminology.hl7.org/CodeSystem/v2-0203", "MR", "Medical Record Number") }
            ],
            Name =
            [
                new HumanName { Family = input.LastName, Given = [input.FirstName], Use = HumanName.NameUse.Official }
            ],
            Active = true
        };

        if (input.DateOfBirth.HasValue)
            patient.BirthDate = input.DateOfBirth.Value.ToString("yyyy-MM-dd");

        if (!string.IsNullOrEmpty(input.Gender))
            patient.Gender = MapGender(input.Gender);

        if (!string.IsNullOrEmpty(input.PersonNumber))
            patient.Identifier.Add(new Identifier("urn:dialysis:person-number", input.PersonNumber));

        return patient;
    }

    private static AdministrativeGender MapGender(string hl7Gender)
    {
        return hl7Gender.ToUpperInvariant() switch
        {
            "M" => AdministrativeGender.Male,
            "F" => AdministrativeGender.Female,
            "O" => AdministrativeGender.Other,
            _ => AdministrativeGender.Unknown
        };
    }
}
