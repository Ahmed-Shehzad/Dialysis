namespace Dialysis.SmartConnect.Alerts;

/// <summary>Outcome of <see cref="IAlertActionProvider.ExecuteAsync"/>. Captured into an <see cref="AlertActionOutcome"/> row.</summary>
public sealed class AlertActionResult
{
    public bool Succeeded { get; init; }

    public string? ErrorDetail { get; init; }

    public string? ResponseSummary { get; init; }

    public static AlertActionResult Success(string? summary = null) => new() { Succeeded = true, ResponseSummary = summary };

    public static AlertActionResult Failure(string detail) => new() { Succeeded = false, ErrorDetail = detail };
}
