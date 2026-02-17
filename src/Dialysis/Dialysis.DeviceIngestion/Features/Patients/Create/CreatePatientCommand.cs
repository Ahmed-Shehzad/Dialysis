using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Patients.Create;

public sealed record CreatePatientCommand(
    TenantId TenantId,
    PatientId LogicalId,
    string? FamilyName,
    string? GivenNames,
    DateTime? BirthDate) : ICommand<CreatePatientResult>;

public sealed record CreatePatientResult(PatientId LogicalId);
