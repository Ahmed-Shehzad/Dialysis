namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Lifecycle of a row on the command ledger. <see cref="Applied"/> is the only terminal-success
/// state; <see cref="Failed"/> is terminal after the consumer exhausts retries and sends the
/// envelope to the dead-letter queue.
/// </summary>
public enum CommandLedgerStatus
{
    /// <summary>Row has been claimed by the consumer; the handler has not yet committed.</summary>
    Pending = 0,

    /// <summary>The handler ran and the resulting transaction (handler change + this row update) committed.</summary>
    Applied = 1,

    /// <summary>The handler threw a non-transient error; the envelope was dead-lettered. The result endpoint surfaces <see cref="CommandLedgerEntry.FailureJson"/>.</summary>
    Failed = 2,
}
