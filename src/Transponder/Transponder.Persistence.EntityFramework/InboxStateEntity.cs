using Transponder.Persistence.Abstractions;

namespace Transponder.Persistence.EntityFramework;

/// <summary>
/// Entity Framework entity for inbox state.
/// </summary>
public sealed class InboxStateEntity : IInboxState
{
    /// <inheritdoc />
    public Ulid MessageId { get; set; }

    /// <inheritdoc />
    public string ConsumerId { get; set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset ReceivedTime { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ProcessedTime { get; set; }

    internal static InboxStateEntity FromState(IInboxState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return new InboxStateEntity
        {
            MessageId = state.MessageId,
            ConsumerId = state.ConsumerId,
            ReceivedTime = state.ReceivedTime,
            ProcessedTime = state.ProcessedTime
        };
    }
}
