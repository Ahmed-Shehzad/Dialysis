using Dialysis.EHR.Contracts.CodeSets;

namespace Dialysis.EHR.Billing.Edi837;

/// <summary>
/// Format-selection entry point for the EDI 837 export path: routes a claim context to the
/// writer matching the claim's <see cref="Domain.Claim.ClaimFormatCode"/>
/// (<see cref="EhrClaimFormats.Edi837Institutional"/> → <see cref="Edi837IClaimWriter"/>;
/// everything else, including the historical default <see cref="EhrClaimFormats.Edi837Professional"/>,
/// stays on <see cref="Edi837PClaimWriter"/> so existing claims are untouched).
/// </summary>
public static class Edi837ClaimWriters
{
    /// <summary>Serialises the claim with the writer matching its format code.</summary>
    public static byte[] Write(Edi837ClaimContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return string.Equals(
            context.Claim.ClaimFormatCode, EhrClaimFormats.Edi837Institutional, StringComparison.OrdinalIgnoreCase)
            ? Edi837IClaimWriter.Write(context)
            : Edi837PClaimWriter.Write(context);
    }
}
