namespace Dialysis.EHR.Billing.ChargeEdits;

/// <summary>
/// Thrown when a <see cref="ChargeAdvisorySeverity.Blocking"/> charge-review edit fires and the command
/// did not carry an acknowledgement. An HTTP surface maps this to <c>422 Unprocessable Entity</c> with the
/// advisory list so an authorized biller can review and, if appropriate, re-submit with an audited override.
/// </summary>
public sealed class ChargeEditBlockedException : Exception
{
    /// <summary>The advisories that held the charge/claim.</summary>
    public IReadOnlyList<ChargeAdvisory> Advisories { get; }

    /// <summary>Creates the exception carrying the blocking advisories.</summary>
    public ChargeEditBlockedException(IReadOnlyList<ChargeAdvisory> advisories)
        : base("The charge was held by one or more charge-review edits.") =>
        Advisories = advisories;
}
