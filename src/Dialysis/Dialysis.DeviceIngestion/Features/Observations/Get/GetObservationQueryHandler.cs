using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Observations.Get;

public sealed class GetObservationQueryHandler : IQueryHandler<GetObservationQuery, Observation?>
{
    private readonly IObservationRepository _observationRepository;

    public GetObservationQueryHandler(IObservationRepository observationRepository)
    {
        _observationRepository = observationRepository;
    }

    public async Task<Observation?> HandleAsync(GetObservationQuery request, CancellationToken cancellationToken = default)
    {
        return await _observationRepository.GetByIdAsync(request.TenantId, request.ObservationId, cancellationToken);
    }
}
