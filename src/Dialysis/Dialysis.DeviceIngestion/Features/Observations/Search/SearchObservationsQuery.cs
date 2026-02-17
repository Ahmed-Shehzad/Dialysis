using Dialysis.Domain.Aggregates;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Observations.Search;

public sealed record SearchObservationsQuery(TenantId TenantId, PatientId PatientId) : IQuery<IReadOnlyList<Observation>>;
