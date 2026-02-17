namespace Dialysis.Gateway.Features.Sessions;

/// <summary>
/// Options for session completion. When UseSaga is true, session completion uses
/// Transponder saga orchestration (EHR push, audit) instead of event choreography.
/// </summary>
public sealed class SessionCompletionOptions
{
    public const string Section = "SessionCompletion";

    /// <summary>
    /// Use saga orchestration when true. Requires EventExport (Azure Service Bus) to be configured.
    /// Default: true when EventExport is configured.
    /// </summary>
    public bool UseSaga { get; set; } = true;
}
