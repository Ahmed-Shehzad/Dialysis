using Dialysis.EHR.Scheduling.Domain;

namespace Dialysis.EHR.Scheduling.Ports;

public interface IProviderAvailabilityRepository
{
    Task<IReadOnlyList<ProviderAvailabilityWindow>> ListByProviderAsync(Guid providerId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    void Add(ProviderAvailabilityWindow window);
}
