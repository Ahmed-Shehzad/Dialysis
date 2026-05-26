namespace Dialysis.SmartConnect.Inbound;

/// <summary>
/// Content-based router. Source connectors call this before dispatching to discover which Started
/// flows match a candidate message (by source-connector kind + message-type pattern + sender id).
///
/// Returning an empty list signals "no subscriptions match". Source connectors typically fall back
/// to <c>DefaultFlowId</c> in that case so legacy single-target wiring keeps working.
/// </summary>
public interface IMessageRouter
{
    Task<IReadOnlyList<Guid>> ResolveFlowIdsAsync(MessageRoutingCandidate candidate, CancellationToken cancellationToken);
}

/// <summary>
/// Inputs the router uses to evaluate <c>InboundSubscriptionSlot</c> against a candidate inbound message.
/// </summary>
public sealed record MessageRoutingCandidate(
    string SourceKind,
    string? MessageType,
    string? SenderId,
    IReadOnlyDictionary<string, string> Metadata);
