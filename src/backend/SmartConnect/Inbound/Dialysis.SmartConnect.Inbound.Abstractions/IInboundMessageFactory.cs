namespace Dialysis.SmartConnect.Inbound;

/// <summary>
/// Builds an <see cref="IntegrationMessage"/> from raw inbound data (transports set Id, time, and correlation).
/// </summary>
public interface IInboundMessageFactory
{
    /// <param name="flowId">Target <see cref="IntegrationMessage.FlowId"/>.</param>
    /// <param name="payload">Raw message body.</param>
    /// <param name="payloadFormat">Declared format of <paramref name="payload"/>.</param>
    /// <param name="correlationId">Optional; when null or whitespace, a new correlation id is generated.</param>
    /// <param name="metadata">Optional key/value metadata (e.g. transport headers).</param>
    /// <param name="receivedAtUtc">Optional receive time; defaults to <see cref="TimeProvider"/> if registered, else <see cref="DateTimeOffset.UtcNow"/>.</param>
    IntegrationMessage Create(
        Guid flowId,
        ReadOnlyMemory<byte> payload,
        PayloadFormat payloadFormat,
        string? correlationId,
        IReadOnlyDictionary<string, string>? metadata = null,
        DateTimeOffset? receivedAtUtc = null);
}
