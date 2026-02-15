using Transponder.Transports.Abstractions;

namespace Transponder.Abstractions;

/// <summary>
/// Provides dead-letter queue functionality for messages that cannot be processed.
/// </summary>
public interface IDeadLetterQueue
{
    /// <summary>
    /// Sends a message to the dead-letter queue.
    /// </summary>
    /// <param name="message">The transport message to dead-letter.</param>
    /// <param name="reason">The reason for dead-lettering the message.</param>
    /// <param name="description">Additional description of the failure.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    Task SendToDeadLetterQueueAsync(
        ITransportMessage message,
        string reason,
        string? description = null,
        CancellationToken cancellationToken = default);
}
