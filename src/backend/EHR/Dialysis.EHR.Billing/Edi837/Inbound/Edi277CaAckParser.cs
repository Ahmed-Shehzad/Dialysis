namespace Dialysis.EHR.Billing.Edi837.Inbound;

/// <summary>
/// Parses an ANSI ASC X12N 277CA Claim Acknowledgement. The 277CA is what the
/// clearinghouse sends after the syntactic 999: it carries the payer-level judgement on
/// each individual claim — accepted (forwarded to payer), rejected (clearinghouse-level
/// failure), or accepted with payer warnings.
///
/// Wire format: ISA &gt; GS &gt; ST*277 &gt; BHT*0085*08 &gt; HL loops nested per claim &gt;
/// STC (status category + reason code) &gt; REF (payer claim control number) &gt; SE &gt;
/// GE &gt; IEA.
///
/// The status category codes follow the X12 BCH list: A0–A3 = accepted at the
/// clearinghouse, A4–A7 = forwarded to payer with payer claim number, F0–F2 =
/// finalized, P0–P5 = pending, R0–R7 = rejected. The parser surfaces a per-claim row
/// keyed on the original 837's CLM01 (claim control number) so the downstream consumer
/// can correlate back to our <see cref="Domain.Claim"/> aggregate.
/// </summary>
public sealed class Edi277CaAckParser
{
    public Edi277CaAckResult Parse(ReadOnlyMemory<byte> ackBytes)
    {
        if (ackBytes.Length == 0)
            throw new ArgumentException("Empty 277CA payload.", nameof(ackBytes));

        var text = System.Text.Encoding.ASCII.GetString(ackBytes.Span);
        var delimiters = Edi999Delimiters.Probe(text);
        var segments = text.Split(delimiters.SegmentTerminator, StringSplitOptions.RemoveEmptyEntries);

        var rows = new List<Edi277CaClaimStatus>();
        string? currentControlNumber = null;
        string? currentPayerClaimNumber = null;
        Edi277Verdict currentVerdict = Edi277Verdict.Pending;
        var currentReasons = new List<string>();

        foreach (var segment in segments)
        {
            var elements = segment.Split(delimiters.ElementSeparator);
            if (elements.Length == 0) continue;
            switch (elements[0])
            {
                case "TRN":
                    // TRN02 — original submitter claim control number (the CLM01 we wrote).
                    if (currentControlNumber is not null)
                        rows.Add(new Edi277CaClaimStatus(
                            currentControlNumber,
                            currentPayerClaimNumber,
                            currentVerdict,
                            currentReasons.ToArray()));
                    currentControlNumber = elements.Length > 2 ? elements[2] : null;
                    currentPayerClaimNumber = null;
                    currentVerdict = Edi277Verdict.Pending;
                    currentReasons = new List<string>();
                    break;
                case "STC":
                    // STC01 = composite (category^reason); STC02 = effective date.
                    if (elements.Length > 1)
                    {
                        var stcParts = elements[1].Split(delimiters.CompositeSeparator);
                        var category = stcParts.Length > 0 ? stcParts[0] : string.Empty;
                        var reason = stcParts.Length > 1 ? stcParts[1] : string.Empty;
                        currentVerdict = MapVerdict(category);
                        if (!string.IsNullOrEmpty(reason))
                            currentReasons.Add($"{category}/{reason}");
                    }
                    break;
                case "REF":
                    // REF*1K = payer-assigned claim control number.
                    if (elements.Length > 2 && elements[1] == "1K")
                        currentPayerClaimNumber = elements[2];
                    break;
            }
        }
        // Flush the last claim row.
        if (currentControlNumber is not null)
            rows.Add(new Edi277CaClaimStatus(
                currentControlNumber,
                currentPayerClaimNumber,
                currentVerdict,
                currentReasons.ToArray()));

        return new Edi277CaAckResult(rows);
    }

    /// <summary>
    /// Maps a BCH (HCC) status-category code to one of three verdict buckets the
    /// downstream consumer recognises. The full BCH list is much richer; the buckets
    /// preserve the clinically-actionable distinction (accepted / pending / rejected)
    /// without the consumer having to track 30+ codes.
    /// </summary>
    private static Edi277Verdict MapVerdict(string category) => category switch
    {
        "A0" or "A1" or "A2" or "A3" or "A4" or "A5" or "A6" or "A7" or "A8" => Edi277Verdict.Accepted,
        "F0" or "F1" or "F2" or "F3F" or "F4" => Edi277Verdict.Accepted,
        "P0" or "P1" or "P2" or "P3" or "P4" or "P5" => Edi277Verdict.Pending,
        _ => Edi277Verdict.Rejected,
    };
}

public enum Edi277Verdict
{
    Pending = 0,
    Accepted = 1,
    Rejected = 2,
}

public sealed record Edi277CaAckResult(IReadOnlyList<Edi277CaClaimStatus> ClaimStatuses);

public sealed record Edi277CaClaimStatus(
    string OriginalClaimControlNumber,
    string? PayerClaimControlNumber,
    Edi277Verdict Verdict,
    IReadOnlyList<string> ReasonCodes);
