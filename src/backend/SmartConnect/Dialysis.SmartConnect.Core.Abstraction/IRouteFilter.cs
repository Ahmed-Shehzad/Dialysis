namespace Dialysis.SmartConnect;

/// <summary>
/// Ordered gate evaluated after an inbound message is accepted for a flow.
/// </summary>
public interface IRouteFilter
{
    string Kind { get; }

    Task<RouteFilterResult> EvaluateAsync(IntegrationMessage message, CancellationToken cancellationToken);
}
