using BuildingBlocks.ValueObjects;

using Dialysis.Patient.Application.Domain.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Patient.Application.Features.RegisterPatient;

public sealed record RegisterPatientCommand(
    MedicalRecordNumber MedicalRecordNumber,
    Person Name,
    DateOnly? DateOfBirth,
    Gender? Gender) : ICommand<RegisterPatientResponse>;
