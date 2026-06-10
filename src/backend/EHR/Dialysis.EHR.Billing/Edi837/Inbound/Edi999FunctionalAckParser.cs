using System.Text;

namespace Dialysis.EHR.Billing.Edi837.Inbound;

/// <summary>
/// Parses an ANSI ASC X12N 999 Functional Acknowledgement. The 999 is sent by the
/// clearinghouse to report whether the syntactic structure of the outbound 837
/// transaction set was accepted or rejected. It does not carry any payer judgement —
/// that comes later in the 277CA.
///
/// Wire format: ISA &gt; GS &gt; ST*999 &gt; AK1 (functional group) &gt; AK2 (transaction
/// set) &gt; IK3 (segment-level error) &gt; IK4 (element-level error) &gt; IK5
/// (transaction-set summary) &gt; AK9 (functional-group summary) &gt; SE &gt; GE &gt; IEA.
///
/// The parser reads the segments byte-by-byte rather than allocating a full DOM —
/// 999 acks are small (typically a few hundred bytes) and the writer needs only the
/// accept/reject verdict plus the original 837 control numbers to correlate back to the
/// outbound <see cref="Domain.Claim"/>.
/// </summary>
public sealed class Edi999FunctionalAckParser
{
    public static Edi999AckResult Parse(ReadOnlyMemory<byte> ackBytes)
    {
        if (ackBytes.Length == 0)
            throw new ArgumentException("Empty 999 payload.", nameof(ackBytes));

        var text = Encoding.ASCII.GetString(ackBytes.Span);
        var delimiters = Edi999Delimiters.Probe(text);
        var segments = text.Split(delimiters.SegmentTerminator, StringSplitOptions.RemoveEmptyEntries);

        string? originalGroupControlNumber = null;
        string? originalTransactionControlNumber = null;
        var verdict = Edi999Verdict.Accepted;
        var errors = new List<string>();

        foreach (var segment in segments)
        {
            var elements = segment.Split(delimiters.ElementSeparator);
            if (elements.Length == 0)
                continue;
            switch (elements[0])
            {
                case "AK1":
                    if (elements.Length > 2)
                        originalGroupControlNumber = elements[2];
                    break;
                case "AK2":
                    if (elements.Length > 2)
                        originalTransactionControlNumber = elements[2];
                    break;
                case "IK3":
                case "IK4":
                    errors.Add(string.Join(' ', elements.Skip(1)));
                    break;
                case "IK5":
                    // IK501 = transaction-set acknowledgement code (A=accept, E=accept-with-errors,
                    // M=accept-with-errors-but-process, R=reject, X=reject-with-errors-mandatory-fix).
                    if (elements.Length > 1)
                    {
                        verdict = elements[1] switch
                        {
                            "A" => Edi999Verdict.Accepted,
                            "E" or "M" => Edi999Verdict.AcceptedWithErrors,
                            _ => Edi999Verdict.Rejected,
                        };
                    }
                    break;
                case "AK9":
                    if (elements.Length > 1 && (elements[1] == "R" || elements[1] == "X"))
                        verdict = Edi999Verdict.Rejected;
                    break;
            }
        }

        return new Edi999AckResult(
            verdict,
            originalGroupControlNumber,
            originalTransactionControlNumber,
            errors);
    }
}

public enum Edi999Verdict
{
    Accepted = 0,
    AcceptedWithErrors = 1,
    Rejected = 2,
}

public sealed record Edi999AckResult
{
    public Edi999AckResult(Edi999Verdict Verdict,
        string? OriginalGroupControlNumber,
        string? OriginalTransactionControlNumber,
        IReadOnlyList<string> Errors)
    {
        this.Verdict = Verdict;
        this.OriginalGroupControlNumber = OriginalGroupControlNumber;
        this.OriginalTransactionControlNumber = OriginalTransactionControlNumber;
        this.Errors = Errors;
    }
    public Edi999Verdict Verdict { get; init; }
    public string? OriginalGroupControlNumber { get; init; }
    public string? OriginalTransactionControlNumber { get; init; }
    public IReadOnlyList<string> Errors { get; init; }
    public void Deconstruct(out Edi999Verdict Verdict, out string? OriginalGroupControlNumber, out string? OriginalTransactionControlNumber, out IReadOnlyList<string> Errors)
    {
        Verdict = this.Verdict;
        OriginalGroupControlNumber = this.OriginalGroupControlNumber;
        OriginalTransactionControlNumber = this.OriginalTransactionControlNumber;
        Errors = this.Errors;
    }
}

/// <summary>
/// X12 declares its delimiters in the first ISA segment: position 4 = element separator,
/// position 105 = component-element separator, position 106 = segment terminator. We
/// honour the actual declared delimiters rather than hard-coding <c>~</c> / <c>*</c>
/// because some clearinghouses send different conventions.
/// </summary>
internal readonly record struct Edi999Delimiters
{
    /// <summary>
    /// X12 declares its delimiters in the first ISA segment: position 4 = element separator,
    /// position 105 = component-element separator, position 106 = segment terminator. We
    /// honour the actual declared delimiters rather than hard-coding <c>~</c> / <c>*</c>
    /// because some clearinghouses send different conventions.
    /// </summary>
    public Edi999Delimiters(char ElementSeparator, char CompositeSeparator, char SegmentTerminator)
    {
        this.ElementSeparator = ElementSeparator;
        this.CompositeSeparator = CompositeSeparator;
        this.SegmentTerminator = SegmentTerminator;
    }
    public static Edi999Delimiters Probe(string text)
    {
        if (text.Length < 106 || !text.StartsWith("ISA", StringComparison.Ordinal))
            return Default;
        return new Edi999Delimiters(
            ElementSeparator: text[3],
            CompositeSeparator: text[104],
            SegmentTerminator: text[105]);
    }

    public static Edi999Delimiters Default { get; } = new('*', ':', '~');
    public char ElementSeparator { get; init; }
    public char CompositeSeparator { get; init; }
    public char SegmentTerminator { get; init; }
    public void Deconstruct(out char ElementSeparator, out char CompositeSeparator, out char SegmentTerminator)
    {
        ElementSeparator = this.ElementSeparator;
        CompositeSeparator = this.CompositeSeparator;
        SegmentTerminator = this.SegmentTerminator;
    }
}
