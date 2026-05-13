using Dialysis.EHR.Integration.Domain;
using Dialysis.EHR.Integration.Ports;

namespace Dialysis.EHR.Integration.Adapters;

public sealed class NoopPharmacyGateway : IPharmacyGateway
{
    public Task<string> TransmitAsync(PharmacyTransmission transmission, CancellationToken cancellationToken) =>
        Task.FromResult($"noop-{transmission.Id:N}");
}

public sealed class NoopLabGateway : ILabGateway
{
    public Task<string> TransmitAsync(LabTransmission transmission, CancellationToken cancellationToken) =>
        Task.FromResult($"noop-{transmission.Id:N}");
}

public sealed class NoopInsurerGateway : IInsurerGateway
{
    public Task TransmitAsync(InsurerTransmission transmission, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
