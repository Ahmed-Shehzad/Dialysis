namespace Dialysis.BuildingBlocks.Transponder.RoutingSlips;

/// <summary>
/// Starts a durable routing slip: inserts saga-backed state and publishes the first <see cref="TransponderRoutingSlipContinue"/>.
/// </summary>
public interface ITransponderRoutingSlipStarter
{
    /// <summary>
    /// Creates a new slip id, persists initial state, and publishes the first continue message.
    /// </summary>
    /// <returns>The slip id (also used as saga instance key).</returns>
    Task<string> StartAsync(
        IReadOnlyList<TransponderRoutingSlipActivityRef> itinerary,
        TransponderPublishOptions? publishOptions = null,
        CancellationToken cancellationToken = default);
}
