using System.Text;

namespace Dialysis.EHR.Billing.Edi837;

/// <summary>
/// Segment helpers shared by the <see cref="Edi837PClaimWriter"/> (837P professional) and
/// <see cref="Edi837IClaimWriter"/> (837I institutional). The two transaction sets share the
/// same ISA/GS/ST envelope, the 1000A/B submitter+receiver loops, the 2000A billing-provider
/// and 2000B subscriber hierarchy, and the trailer arithmetic — only the implementation-guide
/// reference (GS08/ST03) and the claim/service-line loops differ, so those stay on the
/// per-variant writers. Extracted verbatim from the original 837P writer so the professional
/// output is byte-for-byte unchanged.
///
/// Delimiters follow the X12 convention: <c>~</c> segment, <c>*</c> element,
/// <c>:</c> composite. The first ISA segment encodes the delimiters into bytes 4 and
/// onwards so any conformant parser auto-detects them.
/// </summary>
internal static class Edi837SegmentWriter
{
    internal const char SegmentTerminator = '~';
    internal const char ElementSeparator = '*';
    internal const char CompositeSeparator = ':';

    internal static void WriteIsa(StringBuilder sb, Edi837ClaimContext ctx)
    {
        var now = ctx.GeneratedAtUtc;
        // ISA segment is fixed-width per X12 spec — every element is padded to its
        // declared length. The control numbers are 9-digit zero-padded integers.
        var isa = $"ISA*00*          *00*          *ZZ*{Pad(ctx.SubmitterId, 15)}*ZZ*{Pad(ctx.ReceiverId, 15)}*{now:yyMMdd}*{now:HHmm}*^*00501*{ctx.InterchangeControlNumber:D9}*0*P*{CompositeSeparator}{SegmentTerminator}";
        sb.Append(isa);
    }

    internal static void WriteGs(StringBuilder sb, Edi837ClaimContext ctx, string implementationGuideReference) =>
        // HC = Health Care Claim. The functional control number must match GE at the end.
        sb.Append($"GS*HC*{ctx.SubmitterId}*{ctx.ReceiverId}*{ctx.GeneratedAtUtc:yyyyMMdd}*{ctx.GeneratedAtUtc:HHmm}*{ctx.GroupControlNumber}*X*{implementationGuideReference}{SegmentTerminator}");

    internal static void WriteStBht(StringBuilder sb, Edi837ClaimContext ctx, string implementationGuideReference)
    {
        // ST01 = 837 (transaction-set identifier); ST03 = the version reference.
        sb.Append($"ST*837*{ctx.TransactionControlNumber:D4}*{implementationGuideReference}{SegmentTerminator}");
        // BHT = beginning-of-hierarchical-transaction. 0019 = "Information Source",
        // 00 = "Original" (vs 18 = "Reissue").
        sb.Append($"BHT*0019*00*{ctx.Claim.Id:N}*{ctx.GeneratedAtUtc:yyyyMMdd}*{ctx.GeneratedAtUtc:HHmm}*CH{SegmentTerminator}");
    }

    internal static void WriteSubmitter1000A(StringBuilder sb, Edi837ClaimContext ctx)
    {
        // 1000A — submitter (the dialysis operator's billing entity).
        sb.Append($"NM1*41*2*{ctx.SubmitterName}*****46*{ctx.SubmitterId}{SegmentTerminator}");
        sb.Append($"PER*IC*{ctx.SubmitterContactName}*TE*{ctx.SubmitterContactPhone}{SegmentTerminator}");
    }

    internal static void WriteReceiver1000B(StringBuilder sb, Edi837ClaimContext ctx) =>
        // 1000B — receiver (the clearinghouse).
        sb.Append($"NM1*40*2*{ctx.ReceiverName}*****46*{ctx.ReceiverId}{SegmentTerminator}");

    internal static void WriteBillingProvider2000A(StringBuilder sb, Edi837ClaimContext ctx, bool includeTaxonomyPrv)
    {
        // HL = hierarchical level. Level 1 = Billing Provider, 20 = Information Source.
        sb.Append($"HL*1**20*1{SegmentTerminator}");
        // PRV*BI (billing-provider specialty/taxonomy) is an 837P loop-2000A segment; the
        // 837I carries provider roles (attending/operating) in loop 2300 instead.
        if (includeTaxonomyPrv)
            sb.Append($"PRV*BI*PXC*{ctx.BillingProviderTaxonomyCode}{SegmentTerminator}");
        sb.Append($"NM1*85*2*{ctx.BillingProviderName}*****XX*{ctx.BillingProviderNpi}{SegmentTerminator}");
        sb.Append($"N3*{ctx.BillingProviderAddress.Line1}{SegmentTerminator}");
        sb.Append($"N4*{ctx.BillingProviderAddress.City}*{ctx.BillingProviderAddress.StateOrProvince}*{ctx.BillingProviderAddress.PostalCode}{SegmentTerminator}");
        sb.Append($"REF*EI*{ctx.BillingProviderTaxId}{SegmentTerminator}");
    }

    internal static void WriteSubscriber2000B(StringBuilder sb, Edi837ClaimContext ctx)
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

    internal static void WriteDiagnosisHi(StringBuilder sb, Edi837ClaimContext ctx)
    {
        // Diagnosis codes (HI segment) — ICD-10-CM (ABK qualifier for the principal
        // diagnosis, ABF for additional). The qualifiers are common to 837P and 837I.
        if (ctx.DiagnosisCodes.Count == 0)
            return;
        sb.Append("HI");
        for (var i = 0; i < ctx.DiagnosisCodes.Count; i++)
        {
            var qualifier = i == 0 ? "ABK" : "ABF";
            sb.Append(ElementSeparator).Append(qualifier).Append(CompositeSeparator).Append(ctx.DiagnosisCodes[i]);
        }
        sb.Append(SegmentTerminator);
    }

    internal static void WriteSe(StringBuilder sb, Edi837ClaimContext ctx)
    {
        // SE01 = number of segments in the transaction set including ST and SE. We
        // count by re-scanning the buffer rather than trusting hand-maintained counts —
        // the inbound 999 parser will reject the transaction if SE01 is wrong.
        var segments = CountSegments(sb);
        sb.Append($"SE*{segments + 1}*{ctx.TransactionControlNumber:D4}{SegmentTerminator}");
    }

    internal static void WriteGe(StringBuilder sb, Edi837ClaimContext ctx) =>
        sb.Append($"GE*1*{ctx.GroupControlNumber}{SegmentTerminator}");

    internal static void WriteIea(StringBuilder sb, Edi837ClaimContext ctx) =>
        sb.Append($"IEA*1*{ctx.InterchangeControlNumber:D9}{SegmentTerminator}");

    private static int CountSegments(StringBuilder sb)
    {
        var count = 0;
        for (var i = 0; i < sb.Length; i++)
            if (sb[i] == SegmentTerminator)
                count++;
        // We started counting from ST; ISA/GS belong to the interchange/group envelope
        // and aren't part of the transaction-set count. Subtract them out.
        // ISA and GS are always exactly 2 segments before ST.
        return count - 2;
    }

    private static string Pad(string value, int width)
    {
        if (value.Length >= width)
            return value[..width];
        return value.PadRight(width);
    }
}
