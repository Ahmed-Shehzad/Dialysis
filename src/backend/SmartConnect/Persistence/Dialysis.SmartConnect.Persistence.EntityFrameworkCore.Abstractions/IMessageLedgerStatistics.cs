namespace Dialysis.SmartConnect.Persistence;

/// <summary>Per-flow message statistics (grouped by <see cref="MessageLedgerStatus"/>).</summary>
public interface IMessageLedgerStatistics
{
    Task<IReadOnlyList<FlowStatusCount>> GetFlowStatisticsAsync(Guid flowId, CancellationToken cancellationToken);
}

/// <summary>A count of ledger entries for a given status within a flow.</summary>
public sealed class FlowStatusCount
{
    public required MessageLedgerStatus Status { get; init; }
    public required long Count { get; init; }
}
