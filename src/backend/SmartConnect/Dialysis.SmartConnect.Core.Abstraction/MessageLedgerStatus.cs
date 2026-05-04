namespace Dialysis.SmartConnect;

/// <summary>
/// Append-only ledger stage for observability and replay.
/// </summary>
public enum MessageLedgerStatus
{
    Received = 0,
    RouteFilterDropped = 1,
    OutboundSent = 2,
    OutboundFailed = 3,
    Completed = 4,
}
