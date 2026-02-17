using Dialysis.Domain.Aggregates;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Observations.Get;

public sealed record GetObservationQuery(TenantId TenantId, ObservationId ObservationId) : IQuery<Observation?>;
