using Dialysis.EHR.Registration.Domain;

namespace Dialysis.EHR.Registration.Ports;

public interface IProviderRepository
{
    Task<Provider?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Provider?> FindByNpiAsync(string npi, CancellationToken cancellationToken = default);

    void Add(Provider provider);
}
