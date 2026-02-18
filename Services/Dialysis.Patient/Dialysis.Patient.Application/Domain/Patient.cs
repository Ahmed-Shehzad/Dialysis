using BuildingBlocks;
using BuildingBlocks.Tenancy;
using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Domain.Events;
using Dialysis.Patient.Application.Domain.ValueObjects;

namespace Dialysis.Patient.Application.Domain;

public sealed class Patient : AggregateRoot
{
    public string TenantId { get; private set; } = TenantContext.DefaultTenantId;
    public MedicalRecordNumber MedicalRecordNumber { get; private set; }
    public string? PersonNumber { get; private set; }
    public string? SocialSecurityNumber { get; private set; }
    public Person Name { get; private set; } = null!;
    public DateOnly? DateOfBirth { get; private set; }
    public Gender? Gender { get; private set; }

    private Patient() { }

    public static Patient Register(
        MedicalRecordNumber medicalRecordNumber,
        Person name,
        DateOnly? dateOfBirth,
        Gender? gender,
        string? tenantId = null,
        string? personNumber = null,
        string? socialSecurityNumber = null)
    {
        var patient = new Patient
        {
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? TenantContext.DefaultTenantId : tenantId,
            MedicalRecordNumber = medicalRecordNumber,
            PersonNumber = personNumber,
            SocialSecurityNumber = socialSecurityNumber,
            Name = name,
            DateOfBirth = dateOfBirth,
            Gender = gender
        };

        patient.ApplyEvent(new PatientRegisteredEvent(patient.Id, medicalRecordNumber, name));
        return patient;
    }

    public void UpdateDemographics(
        string? personNumber,
        string? socialSecurityNumber,
        Person? name,
        DateOnly? dateOfBirth,
        Gender? gender)
    {
        if (name is not null) Name = name;
        PersonNumber = personNumber ?? PersonNumber;
        SocialSecurityNumber = socialSecurityNumber ?? SocialSecurityNumber;
        DateOfBirth = dateOfBirth ?? DateOfBirth;
        Gender = gender ?? Gender;
        ApplyUpdateDateTime();
        ApplyEvent(new PatientDemographicsUpdatedEvent(Id, Name));
    }
}
