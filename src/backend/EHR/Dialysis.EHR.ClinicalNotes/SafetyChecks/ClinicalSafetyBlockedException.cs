namespace Dialysis.EHR.ClinicalNotes.SafetyChecks;

/// <summary>
/// Thrown by an order handler when a <see cref="AdvisorySeverity.Blocking"/> advisory was raised and the
/// command did not carry an acknowledgement. The API maps this to <c>422 Unprocessable Entity</c> with the
/// advisory list so the clinician can review and, if appropriate, re-submit with an audited override.
/// </summary>
public sealed class ClinicalSafetyBlockedException : Exception
{
    /// <summary>The advisories that blocked the order.</summary>
    public IReadOnlyList<SafetyAdvisory> Advisories { get; }

    /// <summary>Creates the exception carrying the blocking advisories.</summary>
    public ClinicalSafetyBlockedException(IReadOnlyList<SafetyAdvisory> advisories)
        : base("The order was blocked by one or more clinical safety advisories.") =>
        Advisories = advisories;
}
