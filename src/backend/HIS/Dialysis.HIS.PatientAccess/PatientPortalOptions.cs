namespace Dialysis.HIS.PatientAccess;

/// <summary>Bound from <c>His:PatientAccess</c>. Tightens portal consent when enabled.</summary>
public sealed class PatientPortalOptions
{
    /// <summary>When true, patients without a <c>PortalConsentPreference</c> row cannot use the portal (default: missing row = legacy allow-all).</summary>
    public bool RequireExplicitConsentRowForPortal { get; set; }
}
