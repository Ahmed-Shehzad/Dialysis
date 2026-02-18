namespace Transponder.Abstractions;

/// <summary>
/// Represents a scheduled message that can be canceled.
/// </summary>
public interface IScheduledMessageHandle
{
    /// <summary>
    /// Gets the scheduling token identifier.
    /// </summary>
    Ulid TokenId { get; }

    /// <summary>
    /// Cancels the scheduled message.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    Task CancelAsync(CancellationToken cancellationToken = default);
}
