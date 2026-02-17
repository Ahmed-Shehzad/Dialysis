using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Patients.Delete;

public sealed record DeletePatientCommand(TenantId TenantId, PatientId LogicalId) : ICommand<bool>;
