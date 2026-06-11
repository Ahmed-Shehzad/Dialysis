using System.Globalization;
using System.Text;
using Dialysis.EHR.Billing.Domain;

namespace Dialysis.EHR.Billing.Edi837;

/// <summary>
/// Serialises a <see cref="Claim"/> + its child <see cref="Charge"/> rows into ANSI ASC
/// X12N 837 Institutional (837I) byte content per the 5010 TR3 (X223A2) — the electronic
/// UB-04. This is how freestanding ESRD facilities commonly bill: the 2300 CLM carries the
/// UB-04 type-of-bill composite instead of a place-of-service code, DTP*434 carries the
/// statement period, HI segments carry ICD-10-CM diagnoses (ABK/ABF) <em>and</em>
/// ICD-10-PCS procedures (BBR principal / BBQ other), and each 2400 service line is an SV2
/// keyed by revenue code (e.g. <c>0821</c> hemodialysis) with an optional HCPCS/CPT composite.
///
/// Envelope and the shared 1000A/B + 2000A/B loops come from <see cref="Edi837SegmentWriter"/>,
/// so the institutional writer differs from <see cref="Edi837PClaimWriter"/> only where the
/// implementation guides actually diverge. The claim's institutional inputs (type of bill,
/// statement period, admission, procedures) ride on <see cref="Claim.Institutional"/>;
/// revenue codes ride on <see cref="Charge.RevenueCode"/>.
/// </summary>
public sealed class Edi837IClaimWriter
{
    private const char SegmentTerminator = Edi837SegmentWriter.SegmentTerminator;
    private const char ElementSeparator = Edi837SegmentWriter.ElementSeparator;
    private const char CompositeSeparator = Edi837SegmentWriter.CompositeSeparator;

    /// <summary>GS08/ST03 implementation-guide reference for the 5010 837I (X223A2).</summary>
    internal const string ImplementationGuideReference = "005010X223A2";

    public static byte[] Write(Edi837ClaimContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var institutional = context.Claim.Institutional
            ?? throw new InvalidOperationException(
                "837I output requires institutional claim details (type of bill, statement period) on the claim.");

        var sb = new StringBuilder(4096);
        Edi837SegmentWriter.WriteIsa(sb, context);
        Edi837SegmentWriter.WriteGs(sb, context, ImplementationGuideReference);
        Edi837SegmentWriter.WriteStBht(sb, context, ImplementationGuideReference);
        Edi837SegmentWriter.WriteSubmitter1000A(sb, context);
        Edi837SegmentWriter.WriteReceiver1000B(sb, context);
        // The 837I has no PRV*BI taxonomy segment in 2000A (provider roles live in 2300/2310).
        Edi837SegmentWriter.WriteBillingProvider2000A(sb, context, includeTaxonomyPrv: false);
        Edi837SegmentWriter.WriteSubscriber2000B(sb, context);
        WriteClaim2300(sb, context, institutional);
        WriteServiceLines2400(sb, context);
        Edi837SegmentWriter.WriteSe(sb, context);
        Edi837SegmentWriter.WriteGe(sb, context);
        Edi837SegmentWriter.WriteIea(sb, context);
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static void WriteClaim2300(StringBuilder sb, Edi837ClaimContext ctx, InstitutionalClaimDetails institutional)
    {
        // CLM01 = patient control number (the claim id), CLM02 = total claim amount.
        // CLM05 is the UB-04 type-of-bill composite: CLM05-1 = facility type + care type
        // (TOB digits 2-3, the leading zero is dropped on the wire), CLM05-2 = "A"
        // (Uniform Billing claim-form code — "B" on the professional), CLM05-3 = the
        // claim frequency (TOB digit 4). E.g. TOB 0721 -> 72:A:1.
        var totalAmount = ctx.Claim.BilledTotal.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        var facilityCode = institutional.TypeOfBill[1..3];
        var frequencyCode = institutional.TypeOfBill[3..];
        sb.Append($"CLM*{ctx.Claim.Id:N}*{totalAmount}***{facilityCode}{CompositeSeparator}A{CompositeSeparator}{frequencyCode}*Y*A*Y*I{SegmentTerminator}");

        // Statement-covers period (DTP qualifier 434, range format RD8) — UB-04 FL6.
        var statementFrom = institutional.StatementFromUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var statementTo = institutional.StatementToUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        sb.Append($"DTP*434*RD8*{statementFrom}-{statementTo}{SegmentTerminator}");

        // Admission date (DTP qualifier 435) + CL1 institutional claim codes, where applicable.
        if (institutional.AdmissionDateUtc is { } admission)
            sb.Append($"DTP*435*D8*{admission.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}{SegmentTerminator}");
        if (institutional.AdmissionTypeCode is { } admissionType)
            sb.Append($"CL1*{admissionType}{SegmentTerminator}");

        // ICD-10-CM diagnoses — ABK principal / ABF other, same qualifiers as the 837P.
        Edi837SegmentWriter.WriteDiagnosisHi(sb, ctx);

        // ICD-10-PCS procedures — principal procedure (BBR) gets its own HI segment per the
        // TR3; other procedures (BBQ) share a second HI.
        if (institutional.PrincipalProcedureCode is { } principal)
            sb.Append($"HI*BBR{CompositeSeparator}{principal}{SegmentTerminator}");
        if (institutional.OtherProcedureCodes.Count > 0)
        {
            sb.Append("HI");
            foreach (var code in institutional.OtherProcedureCodes)
                sb.Append(ElementSeparator).Append("BBQ").Append(CompositeSeparator).Append(code);
            sb.Append(SegmentTerminator);
        }
    }

    private static void WriteServiceLines2400(StringBuilder sb, Edi837ClaimContext ctx)
    {
        var lineNumber = 0;
        foreach (var charge in ctx.Charges)
        {
            lineNumber++;
            var revenueCode = charge.RevenueCode
                ?? throw new InvalidOperationException(
                    $"Charge '{charge.Id}' has no revenue code — every 837I service line (SV2) is keyed by one.");
            // LX = service line counter; required at the top of each 2400 loop.
            sb.Append($"LX*{lineNumber}{SegmentTerminator}");
            // SV2 = institutional service line. SV201 = revenue code, SV202 = optional
            // HCPCS/CPT composite (HC qualifier), SV203 = charge amount, SV204 = unit of
            // measure (UN), SV205 = unit count.
            var amount = charge.BilledAmount.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            sb.Append($"SV2*{revenueCode}*HC{CompositeSeparator}{charge.CptCode}*{amount}*UN*1{SegmentTerminator}");
            sb.Append($"DTP*472*D8*{ctx.ServicePeriodEndUtc:yyyyMMdd}{SegmentTerminator}");
        }
    }
}
