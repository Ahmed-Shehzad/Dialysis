using System.Globalization;
using System.Text;
using Dialysis.EHR.Billing.Domain;

namespace Dialysis.EHR.Billing.Edi837;

/// <summary>
/// Serialises a <see cref="Claim"/> + its child <see cref="Charge"/> rows into ANSI ASC
/// X12N 837 Professional (837P) byte content per the TR3 implementation guide. This is
/// the format US payers and clearinghouses accept for professional service claims; the
/// closely-related 837I variant for institutional claims lives in
/// <see cref="Edi837IClaimWriter"/> and shares the envelope/loop helpers via
/// <see cref="Edi837SegmentWriter"/>.
///
/// The X12 envelope is hierarchical: ISA &gt; GS &gt; ST &gt; BHT &gt; loops &gt; SE &gt; GE
/// &gt; IEA. The writer fills out the minimum-required loops the standard requires for a
/// well-formed transaction (Loops 1000A/B Submitter+Receiver, 2000A Billing Provider,
/// 2000B Subscriber, 2300 Claim, 2400 Service Line). Optional information loops that
/// don't apply to a renal-dialysis claim (anaesthesia, durable equipment, repriced
/// amounts, ambulance) are intentionally omitted.
///
/// Delimiters follow the X12 convention: <c>~</c> segment, <c>*</c> element,
/// <c>:</c> composite. The first ISA segment encodes the delimiters into bytes 4 and
/// onwards so any conformant parser auto-detects them.
/// </summary>
public sealed class Edi837PClaimWriter
{
    private const char SegmentTerminator = Edi837SegmentWriter.SegmentTerminator;
    private const char CompositeSeparator = Edi837SegmentWriter.CompositeSeparator;

    /// <summary>GS08/ST03 implementation-guide reference for the 5010 837P (X222A1).</summary>
    internal const string ImplementationGuideReference = "005010X222A1";

    public static byte[] Write(Edi837ClaimContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var sb = new StringBuilder(4096);
        Edi837SegmentWriter.WriteIsa(sb, context);
        Edi837SegmentWriter.WriteGs(sb, context, ImplementationGuideReference);
        Edi837SegmentWriter.WriteStBht(sb, context, ImplementationGuideReference);
        Edi837SegmentWriter.WriteSubmitter1000A(sb, context);
        Edi837SegmentWriter.WriteReceiver1000B(sb, context);
        Edi837SegmentWriter.WriteBillingProvider2000A(sb, context, includeTaxonomyPrv: true);
        Edi837SegmentWriter.WriteSubscriber2000B(sb, context);
        WriteClaim2300(sb, context);
        WriteServiceLines2400(sb, context);
        Edi837SegmentWriter.WriteSe(sb, context);
        Edi837SegmentWriter.WriteGe(sb, context);
        Edi837SegmentWriter.WriteIea(sb, context);
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static void WriteClaim2300(StringBuilder sb, Edi837ClaimContext ctx)
    {
        // CLM01 = patient control number (we use the claim id), CLM02 = total claim amount,
        // CLM05 = facility code (11 = office), CLM06 = signature on file (Y),
        // CLM07 = provider accepts assignment (A), CLM08 = release of information (Y).
        var totalAmount = ctx.Claim.BilledTotal.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        sb.Append($"CLM*{ctx.Claim.Id:N}*{totalAmount}***{ctx.PlaceOfServiceCode}{CompositeSeparator}B{CompositeSeparator}1*Y*A*Y*I{SegmentTerminator}");
        // Date of service window (DTP qualifier 434 = statement-period).
        var serviceDateFrom = ctx.ServicePeriodStartUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var serviceDateTo = ctx.ServicePeriodEndUtc.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        sb.Append($"DTP*434*RD8*{serviceDateFrom}-{serviceDateTo}{SegmentTerminator}");
        Edi837SegmentWriter.WriteDiagnosisHi(sb, ctx);
    }

    private static void WriteServiceLines2400(StringBuilder sb, Edi837ClaimContext ctx)
    {
        var lineNumber = 0;
        foreach (var charge in ctx.Charges)
        {
            lineNumber++;
            // LX = service line counter; required at the top of each 2400 loop.
            sb.Append($"LX*{lineNumber}{SegmentTerminator}");
            // SV1 = professional service line. SV101 = composite (HC qualifier + CPT),
            // SV102 = charge amount, SV103 = unit-of-measure (UN), SV104 = unit count (1),
            // SV107 = diagnosis-code-pointer list pointing back to the HI segment indices.
            var amount = charge.BilledAmount.Amount.ToString("0.00", CultureInfo.InvariantCulture);
            sb.Append($"SV1*HC{CompositeSeparator}{charge.CptCode}*{amount}*UN*1***1{SegmentTerminator}");
            sb.Append($"DTP*472*D8*{ctx.ServicePeriodEndUtc:yyyyMMdd}{SegmentTerminator}");
        }
    }
}
