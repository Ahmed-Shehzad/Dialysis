using Dialysis.DomainDrivenDesign.IntegrationEvents;

namespace Dialysis.EHR.Contracts.Integration;

/// <summary>Emitted when a new patient is registered in the EHR (system of record for patient identity).</summary>
public sealed record PatientRegisteredIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid PatientId,
    string MedicalRecordNumber,
    string FamilyName,
    string GivenName,
    DateOnly DateOfBirth,
    string? SexAtBirthCode,
    string? PreferredLanguageCode) : IIntegrationEvent;

/// <summary>Emitted when patient demographics change in a way other modules should re-sync.</summary>
public sealed record PatientDemographicsUpdatedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid PatientId,
    string MedicalRecordNumber,
    string FamilyName,
    string GivenName) : IIntegrationEvent;

/// <summary>Emitted when two patient records are merged (duplicate resolution); subscribers should re-target by surviving id.</summary>
public sealed record PatientsMergedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOn,
    Guid SurvivingPatientId,
    Guid SupersededPatientId,
    string SurvivingMedicalRecordNumber) : IIntegrationEvent;
