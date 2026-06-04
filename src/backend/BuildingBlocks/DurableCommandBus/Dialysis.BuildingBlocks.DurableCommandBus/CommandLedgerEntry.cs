namespace Dialysis.BuildingBlocks.DurableCommandBus;

/// <summary>
/// Per-command idempotency + status row. Lives on the owning module's <c>DbContext</c> in the
/// schema <c>&lt;moduleSlug&gt;_durablecommands</c>; the bus building block ships the entity, each
/// module's EF configuration wires the DbSet and migration. The row is written in the SAME
/// transaction as the handler's aggregate change (via <c>DurableCommandLedgerBehavior</c>) so a
/// crash between the handler's mutation and the ledger insert is impossible.
/// </summary>
public sealed class CommandLedgerEntry
{
    /// <summary>Required by EF.</summary>
    private CommandLedgerEntry() { }

    public CommandLedgerEntry(
        Guid commandId,
        string commandTypeKey,
        string correlationId,
        DateTime enqueuedAtUtc,
        string? requestedBySubject)
    {
        CommandId = commandId;
        CommandTypeKey = commandTypeKey ?? throw new ArgumentNullException(nameof(commandTypeKey));
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        EnqueuedAtUtc = enqueuedAtUtc;
        RequestedBySubject = requestedBySubject;
        Status = CommandLedgerStatus.Pending;
    }

    /// <summary>PK. Caller-supplied; same value across redeliveries.</summary>
    public Guid CommandId { get; private set; }

    public string CommandTypeKey { get; private set; } = default!;

    /// <summary>Client-facing id surfaced in the 202 response and the status endpoint URL.</summary>
    public string CorrelationId { get; private set; } = default!;

    public DateTime EnqueuedAtUtc { get; private set; }

    public DateTime? AppliedAtUtc { get; private set; }

    public CommandLedgerStatus Status { get; private set; }

    /// <summary>JSON-serialized result, when the handler returned one.</summary>
    public string? ResultJson { get; private set; }

    /// <summary>Set when the handler threw and the envelope was dead-lettered.</summary>
    public string? FailureJson { get; private set; }

    /// <summary>Subject (`sub` claim) the enqueueing client authenticated as. The status endpoint authorizes per-row reads against this value.</summary>
    public string? RequestedBySubject { get; private set; }

    /// <summary>Identifier of the consumer instance that processed the row. Useful when investigating poison messages.</summary>
    public string? ConsumerInstanceId { get; private set; }

    public void MarkApplied(DateTime appliedAtUtc, string? resultJson, string consumerInstanceId)
    {
        if (Status != CommandLedgerStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot mark applied; ledger entry for {CommandId} is in state {Status}.");
        Status = CommandLedgerStatus.Applied;
        AppliedAtUtc = appliedAtUtc;
        ResultJson = resultJson;
        ConsumerInstanceId = consumerInstanceId;
    }

    public void MarkFailed(DateTime failedAtUtc, string failureJson, string consumerInstanceId)
    {
        Status = CommandLedgerStatus.Failed;
        AppliedAtUtc = failedAtUtc;
        FailureJson = failureJson;
        ConsumerInstanceId = consumerInstanceId;
    }
}
