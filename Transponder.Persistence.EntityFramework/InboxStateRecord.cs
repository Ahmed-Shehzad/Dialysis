using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// EF Core persistence model for inbox state.
/// </summary>
public sealed class InboxStateRecord : IInboxState
{
    /// <inheritdoc />
    public Ulid MessageId { get; set; }

    /// <inheritdoc />
    public string ConsumerId { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset ReceivedTime { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ProcessedTime { get; set; }

    internal static InboxStateRecord FromState(IInboxState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new InboxStateRecord
        {
            MessageId = state.MessageId,
            ConsumerId = state.ConsumerId,
            ReceivedTime = state.ReceivedTime,
            ProcessedTime = state.ProcessedTime
        };
    }
}
