namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Thrown when a durable-bus operation fails for a reason the caller can act on (unknown
/// command type, broker publish nack, etc.). Distinguishes durable-bus errors from underlying
/// transport / EF exceptions so the API layer can produce a clean 4xx or 503.
/// </summary>
public sealed class DurableCommandException : Exception
{
    public DurableCommandException(string message) : base(message) { }
    public DurableCommandException(string message, Exception innerException) : base(message, innerException) { }
}
