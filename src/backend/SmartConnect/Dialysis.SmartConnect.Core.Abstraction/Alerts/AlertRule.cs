namespace Dialysis.SmartConnect.Alerts;

/// <summary>
/// Operator-defined rule that fires one or more <see cref="AlertActionSlot"/>s when a matching
/// <see cref="AlertTrigger"/> is published. Mirth UG p313 "Edit Alert View".
/// </summary>
public sealed class AlertRule
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public bool Enabled { get; init; } = true;

    public string? Description { get; init; }

    /// <summary>
    /// Flows this rule applies to. Null/empty list means "all flows" (Mirth UG p317 "Alert Enabled Channels").
    /// </summary>
    public IReadOnlyList<Guid>? EnabledFlowIds { get; init; }

    public IReadOnlyList<AlertErrorPattern> ErrorPatterns { get; init; } = [];

    public IReadOnlyList<AlertActionSlot> Actions { get; init; } = [];

    /// <summary>
    /// Suppress repeat firings of the same (ruleId, flowId, errorType) tuple within this window.
    /// Null = no throttling. Default 60s applied at the engine level if omitted.
    /// </summary>
    public TimeSpan? ThrottleWindow { get; init; }

    public int Revision { get; init; } = 1;

    public DateTimeOffset LastModifiedUtc { get; init; }
}
