namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Public metadata facet of a consume-route registration. Transports inject
/// <see cref="IEnumerable{T}"/> of this to discover which message types the host has
/// subscribed to via <c>AddConsumer&lt;TMessage, TConsumer&gt;()</c>, so they can declare
/// the right bindings without each module also calling a transport-specific
/// <c>Listen&lt;T&gt;()</c> for the same types.
/// </summary>
public interface IConsumeRouteMetadata
{
    Type MessageType { get; }
}

internal interface IConsumeRouteContributor : IConsumeRouteMetadata
{
    void Contribute(Dictionary<string, TransponderConsumeRouteEntry> routes);
}
