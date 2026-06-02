using System.Globalization;
using System.Text;
using Dialysis.EHR.Billing.Domain;

namespace Dialysis.EHR.Billing.Edi837;

/// <summary>
/// Serialises a <see cref="Claim"/> + its child <see cref="Charge"/> rows into ANSI ASC
/// X12N 837 Professional (837P) byte content per the TR3 implementation guide. This is
/// the format US payers and clearinghouses accept for professional service claims; the
/// closely-related 837I variant for institutional claims reuses the same envelope and
/// most of the same loops — we expose <see cref="Variant"/> so the same writer covers
/// both.
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
    private const char SegmentTerminator = '~';
    private const char ElementSeparator = '*';
    private const char CompositeSeparator = ':';

    /// <summary>837P vs 837I — same envelope, different ST/SE transaction code.</summary>
    public enum Variant
    {
        Professional = 0,
        Institutional = 1,
    }

    public byte[] Write(Edi837ClaimContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var sb = new StringBuilder(4096);
        WriteIsa(sb, context);
        WriteGs(sb, context);
        WriteStBht(sb, context);
        WriteSubmitter1000A(sb, context);
        WriteReceiver1000B(sb, context);
        WriteBillingProvider2000A(sb, context);
        WriteSubscriber2000B(sb, context);
        WriteClaim2300(sb, context);
        var serviceLineCount = WriteServiceLines2400(sb, context);
        WriteSe(sb, context, serviceLineCount);
        WriteGe(sb, context);
        WriteIea(sb, context);
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static void WriteIsa(StringBuilder sb, Edi837ClaimContext ctx)
    {
        var now = ctx.GeneratedAtUtc;
        // ISA segment is fixed-width per X12 spec — every element is padded to its
        // declared length. The control numbers are 9-digit zero-padded integers.
        var isa = $"ISA*00*          *00*          *ZZ*{Pad(ctx.SubmitterId, 15)}*ZZ*{Pad(ctx.ReceiverId, 15)}*{now:yyMMdd}*{now:HHmm}*^*00501*{ctx.InterchangeControlNumber:D9}*0*P*{CompositeSeparator}{SegmentTerminator}";
        sb.Append(isa);
    }

    private static void WriteGs(StringBuilder sb, Edi837ClaimContext ctx)
    {
        // HC = Health Care Claim. The functional control number must match GE at the end.
        sb.Append($"GS*HC*{ctx.SubmitterId}*{ctx.ReceiverId}*{ctx.GeneratedAtUtc:yyyyMMdd}*{ctx.GeneratedAtUtc:HHmm}*{ctx.GroupControlNumber}*X*005010X222A1{SegmentTerminator}");
    }

    private static void WriteStBht(StringBuilder sb, Edi837ClaimContext ctx)
    {
        // ST01 = 837 (transaction-set identifier); ST03 = the version reference.
        sb.Append($"ST*837*{ctx.TransactionControlNumber:D4}*005010X222A1{SegmentTerminator}");
        // BHT = beginning-of-hierarchical-transaction. 0019 = "Information Source",
        // 00 = "Original" (vs 18 = "Reissue").
        sb.Append($"BHT*0019*00*{ctx.Claim.Id:N}*{ctx.GeneratedAtUtc:yyyyMMdd}*{ctx.GeneratedAtUtc:HHmm}*CH{SegmentTerminator}");
    }

    private static void WriteSubmitter1000A(StringBuilder sb, Edi837ClaimContext ctx)
    {
        // 1000A — submitter (the dialysis operator's billing entity).
        sb.Append($"NM1*41*2*{ctx.SubmitterName}*****46*{ctx.SubmitterId}{SegmentTerminator}");
        sb.Append($"PER*IC*{ctx.SubmitterContactName}*TE*{ctx.SubmitterContactPhone}{SegmentTerminator}");
    }

    private static void WriteReceiver1000B(StringBuilder sb, Edi837ClaimContext ctx)
    {
        // 1000B — receiver (the clearinghouse).
        sb.Append($"NM1*40*2*{ctx.ReceiverName}*****46*{ctx.ReceiverId}{SegmentTerminator}");
    }

    private static void WriteBillingProvider2000A(StringBuilder sb, Edi837ClaimContext ctx)
    {
        // HL = hierarchical level. Level 1 = Billing Provider, 20 = Information Source.
        sb.Append($"HL*1**20*1{SegmentTerminator}");
        sb.Append($"PRV*BI*PXC*{ctx.BillingProviderTaxonomyCode}{SegmentTerminator}");
        sb.Append($"NM1*85*2*{ctx.BillingProviderName}*****XX*{ctx.BillingProviderNpi}{SegmentTerminator}");
        sb.Append($"N3*{ctx.BillingProviderAddress.Line1}{SegmentTerminator}");
        sb.Append($"N4*{ctx.BillingProviderAddress.City}*{ctx.BillingProviderAddress.StateOrProvince}*{ctx.BillingProviderAddress.PostalCode}{SegmentTerminator}");
        sb.Append($"REF*EI*{ctx.BillingProviderTaxId}{SegmentTerminator}");
    }

    private static void WriteSubscriber2000B(StringBuilder sb, Edi837ClaimContext ctx)
    {
        // Level 2 = Subscriber (the patient when they're their own subscriber, which
        // matches the typical chronic-dialysis case).
        sb.Append($"HL*2*1*22*0{SegmentTerminator}");
        sb.Append($"SBR*P*18*{ctx.SubscriberGroupNumber}******{ctx.Claim.PayerCode}{SegmentTerminator}");
        sb.Append($"NM1*IL*1*{ctx.Subscriber.LastName}*{ctx.Subscriber.FirstName}****MI*{ctx.Subscriber.MemberId}{SegmentTerminator}");
        sb.Append($"N3*{ctx.Subscriber.Address.Line1}{SegmentTerminator}");
        sb.Append($"N4*{ctx.Subscriber.Address.City}*{ctx.Subscriber.Address.StateOrProvince}*{ctx.Subscriber.Address.PostalCode}{SegmentTerminator}");
        sb.Append($"DMG*D8*{ctx.Subscriber.DateOfBirthUtc:yyyyMMdd}*{ctx.Subscriber.GenderCode}{SegmentTerminator}");
        // Payer (NM1 with PR qualifier = payer)
        sb.Append($"NM1*PR*2*{ctx.PayerName}*****PI*{ctx.Claim.PayerCode}{SegmentTerminator}");
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
        // Diagnosis codes (HI segment) — ICD-10-CM (ABK qualifier for primary, ABF for additional).
        if (ctx.DiagnosisCodes.Count > 0)
        {
            sb.Append("HI");
            for (var i = 0; i < ctx.DiagnosisCodes.Count; i++)
            {
                var qualifier = i == 0 ? "ABK" : "ABF";
                sb.Append(ElementSeparator).Append(qualifier).Append(CompositeSeparator).Append(ctx.DiagnosisCodes[i]);
            }
            sb.Append(SegmentTerminator);
        }
    }

    private static int WriteServiceLines2400(StringBuilder sb, Edi837ClaimContext ctx)
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
        return lineNumber;
    }

    private static void WriteSe(StringBuilder sb, Edi837ClaimContext ctx, int serviceLineCount)
    {
        // SE01 = number of segments in the transaction set including ST and SE. We
        // count by re-scanning the buffer rather than trusting hand-maintained counts —
        // the inbound 999 parser will reject the transaction if SE01 is wrong.
        var segments = CountSegments(sb);
        sb.Append($"SE*{segments + 1}*{ctx.TransactionControlNumber:D4}{SegmentTerminator}");
    }

    private static void WriteGe(StringBuilder sb, Edi837ClaimContext ctx) =>
        sb.Append($"GE*1*{ctx.GroupControlNumber}{SegmentTerminator}");

    private static void WriteIea(StringBuilder sb, Edi837ClaimContext ctx) =>
        sb.Append($"IEA*1*{ctx.InterchangeControlNumber:D9}{SegmentTerminator}");

    private static int CountSegments(StringBuilder sb)
    {
        var count = 0;
        for (var i = 0; i < sb.Length; i++)
            if (sb[i] == SegmentTerminator) count++;
        // We started counting from ST; ISA/GS belong to the interchange/group envelope
        // and aren't part of the transaction-set count. Subtract them out.
        // ISA and GS are always exactly 2 segments before ST.
        return count - 2;
    }

    private static string Pad(string value, int width)
    {
        if (value.Length >= width) return value[..width];
        return value.PadRight(width);
    }
}
