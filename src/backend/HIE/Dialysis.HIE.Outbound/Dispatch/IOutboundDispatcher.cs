namespace Dialysis.HIE.Outbound.Dispatch;

/// <summary>
/// Pulls pending bundles from the store, calls the partner endpoint, and updates state. Exposed as a port
/// so tests can run a single tick deterministically without spinning up the hosted service.
/// </summary>
public interface IOutboundDispatcher
{
    Task<int> TickAsync(CancellationToken cancellationToken = default);
}
