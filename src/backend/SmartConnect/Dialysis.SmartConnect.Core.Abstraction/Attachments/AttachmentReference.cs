namespace Dialysis.SmartConnect.Attachments;

/// <summary>
/// Encoding/decoding of inline attachment references in a message payload. Format <c>${ATTACH:&lt;guid&gt;}</c>.
/// </summary>
public static class AttachmentReference
{
    public const string Prefix = "${ATTACH:";
    public const string Suffix = "}";

    public static string Format(Guid id) => $"{Prefix}{id:D}{Suffix}";

    public static bool TryParseToken(ReadOnlySpan<char> token, out Guid id)
    {
        id = Guid.Empty;
        if (!token.StartsWith(Prefix, StringComparison.Ordinal) || !token.EndsWith(Suffix, StringComparison.Ordinal))
        {
            return false;
        }

        var body = token[Prefix.Length..^Suffix.Length];
        return Guid.TryParseExact(body, "D", out id);
    }

    /// <summary>
    /// Enumerates token spans inside <paramref name="text"/> in order. Each match yields (startIndex, length, id).
    /// </summary>
    public static IEnumerable<(int Start, int Length, Guid Id)> Scan(string text)
    {
        var i = 0;
        while (i < text.Length)
        {
            var open = text.IndexOf(Prefix, i, StringComparison.Ordinal);
            if (open < 0)
            {
                yield break;
            }

            var close = text.IndexOf(Suffix, open + Prefix.Length, StringComparison.Ordinal);
            if (close < 0)
            {
                yield break;
            }

            var token = text.AsSpan(open, close - open + Suffix.Length);
            if (TryParseToken(token, out var id))
            {
                yield return (open, token.Length, id);
                i = close + Suffix.Length;
            }
            else
            {
                i = open + Prefix.Length;
            }
        }
    }
}
