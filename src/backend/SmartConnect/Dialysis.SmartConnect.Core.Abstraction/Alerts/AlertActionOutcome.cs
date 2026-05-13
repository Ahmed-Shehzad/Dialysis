namespace Dialysis.SmartConnect.Alerts;

/// <summary>Result of running one <see cref="AlertActionSlot"/> while processing an <see cref="AlertEvent"/>.</summary>
public sealed class AlertActionOutcome
{
    public required string Kind { get; init; }

    public bool Succeeded { get; init; }

    public string? ErrorDetail { get; init; }

    public string? ResponseSummary { get; init; }

    public DateTimeOffset AttemptedAtUtc { get; init; }
}
