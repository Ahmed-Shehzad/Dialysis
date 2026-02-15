namespace Transponder.Abstractions;

/// <summary>
/// Marker interface for all messages flowing through Transponder.
/// </summary>
public interface IMessage
{
}

/// <summary>
/// Provides a correlation identifier for messages.
/// </summary>
public interface ICorrelatedMessage : IMessage
{
    /// <summary>
    /// Gets the correlation identifier used to relate messages.
    /// </summary>
    Ulid CorrelationId { get; }
}
