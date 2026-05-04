namespace Dialysis.BuildingBlocks.Transponder;

/// <summary>
/// Well-known header names used across Transponder transports for routing and tracing.
/// </summary>
public static class TransponderTransportHeaderNames
{
    public const string RoutingKey = "Transponder-Routing-Key";
    public const string CorrelationId = "Transponder-Correlation-Id";
    public const string DeduplicationId = "Transponder-Deduplication-Id";
}
