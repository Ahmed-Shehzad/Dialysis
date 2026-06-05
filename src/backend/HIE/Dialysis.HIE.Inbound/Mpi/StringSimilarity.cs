namespace Dialysis.HIE.Inbound.Mpi;

/// <summary>
/// Jaro-Winkler string similarity (0–1) — the standard fuzzy comparator for MPI name matching:
/// tolerant of transpositions/typos and weighting a shared prefix (surnames + given names rarely
/// differ at the start). Comparison is case- and whitespace-insensitive.
/// </summary>
public static class StringSimilarity
{
    /// <summary>Jaro-Winkler similarity of two strings (1 = identical, 0 = nothing in common).</summary>
    public static double JaroWinkler(string? a, string? b)
    {
        var s1 = Normalize(a);
        var s2 = Normalize(b);
        if (s1.Length == 0 && s2.Length == 0)
        {
            return 1.0;
        }
        if (s1.Length == 0 || s2.Length == 0)
        {
            return 0.0;
        }
        if (string.Equals(s1, s2, StringComparison.Ordinal))
        {
            return 1.0;
        }

        var jaro = Jaro(s1, s2);

        // Winkler prefix boost (up to 4 leading chars, scaling factor 0.1).
        var prefix = 0;
        var max = Math.Min(4, Math.Min(s1.Length, s2.Length));
        while (prefix < max && s1[prefix] == s2[prefix])
        {
            prefix++;
        }

        return jaro + (prefix * 0.1 * (1 - jaro));
    }

    private static double Jaro(string s1, string s2)
    {
        var matchDistance = Math.Max(0, (Math.Max(s1.Length, s2.Length) / 2) - 1);
        var s1Matches = new bool[s1.Length];
        var s2Matches = new bool[s2.Length];

        var matches = 0;
        for (var i = 0; i < s1.Length; i++)
        {
            var start = Math.Max(0, i - matchDistance);
            var end = Math.Min(i + matchDistance + 1, s2.Length);
            for (var j = start; j < end; j++)
            {
                if (s2Matches[j] || s1[i] != s2[j])
                {
                    continue;
                }
                s1Matches[i] = true;
                s2Matches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
        {
            return 0.0;
        }

        double transpositions = 0;
        var k = 0;
        for (var i = 0; i < s1.Length; i++)
        {
            if (!s1Matches[i])
            {
                continue;
            }
            while (!s2Matches[k])
            {
                k++;
            }
            if (s1[i] != s2[k])
            {
                transpositions++;
            }
            k++;
        }
        transpositions /= 2;

        var m = (double)matches;
        return ((m / s1.Length) + (m / s2.Length) + ((m - transpositions) / m)) / 3.0;
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
}
