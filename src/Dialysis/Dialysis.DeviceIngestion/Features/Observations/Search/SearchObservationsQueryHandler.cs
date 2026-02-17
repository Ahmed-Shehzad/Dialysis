using Dialysis.Domain.Aggregates;
using Dialysis.Persistence.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.Observations.Search;

public sealed class SearchObservationsQueryHandler : IQueryHandler<SearchObservationsQuery, IReadOnlyList<Observation>>
{
    private readonly IObservationRepository _observationRepository;

    public SearchObservationsQueryHandler(IObservationRepository observationRepository)
    {
        _observationRepository = observationRepository;
    }

    public async Task<IReadOnlyList<Observation>> HandleAsync(SearchObservationsQuery request, CancellationToken cancellationToken = default)
    {
        return await _observationRepository.GetByPatientAsync(request.TenantId, request.PatientId, cancellationToken);
    }
}
