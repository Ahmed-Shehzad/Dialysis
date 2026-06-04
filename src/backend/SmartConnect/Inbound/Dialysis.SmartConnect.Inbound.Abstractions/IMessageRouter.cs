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
public sealed record MessageRoutingCandidate
{
    /// <summary>
    /// Inputs the router uses to evaluate <c>InboundSubscriptionSlot</c> against a candidate inbound message.
    /// </summary>
    public MessageRoutingCandidate(string SourceKind,
        string? MessageType,
        string? SenderId,
        IReadOnlyDictionary<string, string> Metadata)
    {
        this.SourceKind = SourceKind;
        this.MessageType = MessageType;
        this.SenderId = SenderId;
        this.Metadata = Metadata;
    }
    public string SourceKind { get; init; }
    public string? MessageType { get; init; }
    public string? SenderId { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; }
    public void Deconstruct(out string SourceKind, out string? MessageType, out string? SenderId, out IReadOnlyDictionary<string, string> Metadata)
    {
        SourceKind = this.SourceKind;
        MessageType = this.MessageType;
        SenderId = this.SenderId;
        Metadata = this.Metadata;
    }
}
