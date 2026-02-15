using Microsoft.Extensions.Hosting;
using Transponder.Abstractions;

namespace Dialysis.Messaging;

/// <summary>
/// Hosted service that starts and stops the Transponder bus with the application lifecycle.
/// </summary>
public sealed class TransponderHostedService : IHostedService, IAsyncDisposable
{
    private readonly IBusControl _bus;

    public TransponderHostedService(IBusControl bus)
    {
        _bus = bus;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _bus.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _bus.DisposeAsync();
    }

    public ValueTask DisposeAsync() => _bus.DisposeAsync();
}
