using Dialysis.Domain.Entities;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Patients.List;

public sealed record ListPatientsQuery(
    TenantId TenantId,
    string? Family = null,
    string? Given = null,
    int? Count = null,
    int Offset = 0) : IQuery<IReadOnlyList<Patient>>;
