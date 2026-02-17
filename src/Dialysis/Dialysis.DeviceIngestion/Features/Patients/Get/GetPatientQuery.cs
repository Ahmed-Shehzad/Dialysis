using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Patients.Get;

public sealed record GetPatientQuery(TenantId TenantId, PatientId LogicalId) : IQuery<Patient?>;
