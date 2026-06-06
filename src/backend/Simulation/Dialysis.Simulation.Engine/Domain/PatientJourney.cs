namespace Dialysis.Simulation.Engine.Domain;

/// <summary>
/// The deterministic patient at the centre of a session. Generated once when the session starts (from
/// the seed), then linked to the real EHR patient id once the registration step runs. Every driven
/// record traces back to this journey, so nothing the simulation produces is an orphan.
/// </summary>
public sealed class PatientJourney
{
    private PatientJourney()
    {
    }

    private PatientJourney(
        Guid id,
        Guid simulationSessionId,
        string medicalRecordNumber,
        string familyName,
        string givenName,
        DateOnly dateOfBirth,
        string sexAtBirthCode)
    {
        Id = id;
        SimulationSessionId = simulationSessionId;
        MedicalRecordNumber = medicalRecordNumber;
        FamilyName = familyName;
        GivenName = givenName;
        DateOfBirth = dateOfBirth;
        SexAtBirthCode = sexAtBirthCode;
    }

    /// <summary>The journey id (also the workflow's PatientJourneyId lineage value).</summary>
    public Guid Id { get; private set; }

    /// <summary>The owning session.</summary>
    public Guid SimulationSessionId { get; private set; }

    /// <summary>The generated MRN.</summary>
    public string MedicalRecordNumber { get; private set; } = null!;

    /// <summary>Family name.</summary>
    public string FamilyName { get; private set; } = null!;

    /// <summary>Given name.</summary>
    public string GivenName { get; private set; } = null!;

    /// <summary>Date of birth.</summary>
    public DateOnly DateOfBirth { get; private set; }

    /// <summary>Administrative sex-at-birth code.</summary>
    public string SexAtBirthCode { get; private set; } = null!;

    /// <summary>The real EHR patient id once the registration step has run; <c>null</c> beforehand.</summary>
    public Guid? RealPatientId { get; private set; }

    /// <summary>Creates a journey for a session.</summary>
    public static PatientJourney Create(
        Guid id,
        Guid simulationSessionId,
        string medicalRecordNumber,
        string familyName,
        string givenName,
        DateOnly dateOfBirth,
        string sexAtBirthCode) =>
        new(id, simulationSessionId, medicalRecordNumber, familyName, givenName, dateOfBirth, sexAtBirthCode);

    /// <summary>Links the real EHR patient id produced by the registration step.</summary>
    public void LinkRealPatient(Guid realPatientId) => RealPatientId = realPatientId;
}
