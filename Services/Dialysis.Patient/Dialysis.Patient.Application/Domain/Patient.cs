using BuildingBlocks;
using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Domain.Events;
using Dialysis.Patient.Application.Domain.ValueObjects;

namespace Dialysis.Patient.Application.Domain;

public sealed class Patient : AggregateRoot
{
    public MedicalRecordNumber MedicalRecordNumber { get; private set; }
    public string? PersonNumber { get; set; }
    public PersonName Name { get; private set; } = null!;
    public DateOnly? DateOfBirth { get; private set; }
    public Gender? Gender { get; private set; }

    private Patient() { }

    public static Patient Register(
        MedicalRecordNumber medicalRecordNumber,
        PersonName name,
        DateOnly? dateOfBirth,
        Gender? gender)
    {
        var patient = new Patient
        {
            MedicalRecordNumber = medicalRecordNumber,
            Name = name,
            DateOfBirth = dateOfBirth,
            Gender = gender
        };

        patient.ApplyEvent(new PatientRegisteredEvent(patient.Id, medicalRecordNumber, name));
        return patient;
    }

    public void UpdateDemographics(
        string? personNumber,
        PersonName? name,
        DateOnly? dateOfBirth,
        Gender? gender)
    {
        if (name is not null) Name = name;
        PersonNumber = personNumber ?? PersonNumber;
        DateOfBirth = dateOfBirth ?? DateOfBirth;
        Gender = gender ?? Gender;
        ApplyUpdateDateTime();
        ApplyEvent(new PatientDemographicsUpdatedEvent(Id, Name));
    }
}
