namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Async-local flag that lets <c>DurableCommandLedgerBehavior</c> know whether the current
/// handler invocation is being made by the durable consumer (in which case the behavior runs
/// its idempotency + same-transaction logic) or by a regular synchronous caller (in which
/// case the behavior is a no-op and the handler runs unchanged).
/// </summary>
internal static class DurableCommandScope
{
    private static readonly AsyncLocal<Guid?> _current = new();

    public static Guid? CurrentCommandId => _current.Value;

    public static void Activate(Guid commandId) => _current.Value = commandId;

    public static void Clear() => _current.Value = null;
}
