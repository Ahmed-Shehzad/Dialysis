namespace Dialysis.EHR.Billing.Consumers;

/// <summary>
/// Controls automatic charge capture when a clinical encounter closes. Off by default — many sites
/// have coders review professional charges before they're filed, so this matches the "config-driven,
/// off until adopted" posture of the other billing/clinical rule engines.
/// </summary>
public sealed class EncounterChargeAutomationOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Ehr:Billing:EncounterChargeAutomation";

    /// <summary>
    /// When true, closing an encounter auto-captures one <c>Charge</c> per procedure CPT on the
    /// encounter (priced via the fee schedule). Default false.
    /// </summary>
    public bool Enabled { get; set; }
}
