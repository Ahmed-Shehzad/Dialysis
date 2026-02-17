using Dialysis.FhirStore.Data;

namespace Dialysis.FhirStore;

public interface IObservationStore
{
    Task<string> CreateAsync(ObservationEntity entity, CancellationToken cancellationToken = default);
}
