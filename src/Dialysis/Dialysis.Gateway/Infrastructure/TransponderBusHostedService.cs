using Transponder.Abstractions;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// Starts and stops the Transponder bus with the application lifecycle.
/// </summary>
public sealed class TransponderBusHostedService : IHostedService
{
    private readonly IBusControl _bus;

    public TransponderBusHostedService(IBusControl bus)
    {
        _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    }

    public Task StartAsync(CancellationToken cancellationToken) =>
        _bus.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) =>
        _bus.StopAsync(cancellationToken);
}
