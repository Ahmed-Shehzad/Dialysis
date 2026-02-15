namespace Dialysis.Messaging;

/// <summary>
/// Options for Transponder message consumer endpoints.
/// </summary>
public sealed class MessageConsumerOptions<TMessage>
{
    /// <summary>
    /// The input address (e.g. sb://dialysis/hl7-ingest/subscriptions/his-subscription).
    /// </summary>
    public Uri? InputAddress { get; set; }
}
