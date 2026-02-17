using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Patients.Update;

public sealed record UpdatePatientCommand(
    TenantId TenantId,
    PatientId LogicalId,
    string? FamilyName,
    string? GivenNames,
    DateTime? BirthDate) : ICommand<UpdatePatientResult>;

public sealed record UpdatePatientResult(PatientId LogicalId, string? FamilyName, string? GivenNames, DateTime? BirthDate);
