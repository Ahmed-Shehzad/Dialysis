namespace FhirCore.Subscriptions;

public interface ICriteriaMatcher
{
    bool Matches(string criteria, string resourceType, string resourceId, IReadOnlyDictionary<string, string>? searchContext = null);
}

public sealed class CriteriaMatcher : ICriteriaMatcher
{
    public bool Matches(string criteria, string resourceType, string resourceId, IReadOnlyDictionary<string, string>? searchContext = null)
    {
        if (string.IsNullOrWhiteSpace(criteria))
            return false;

        var parts = criteria.Split('?');
        var typePart = parts[0].Trim();
        if (!typePart.Equals(resourceType, StringComparison.OrdinalIgnoreCase))
            return false;

        if (parts.Length == 1)
            return true;

        var queryString = parts[1].Trim();
        if (string.IsNullOrEmpty(queryString))
            return true;

        var criteriaParams = ParseQueryString(queryString);
        if (criteriaParams.Count == 0)
            return true;

        if (searchContext is null)
            return false;

        foreach (var (key, expectedValues) in criteriaParams)
        {
            var normalizedKey = key.ToLowerInvariant();
            if (!searchContext.TryGetValue(normalizedKey, out var actual))
            {
                var altKey = normalizedKey == "patient" ? "subject" : normalizedKey == "subject" ? "patient" : null;
                if (altKey is null || !searchContext.TryGetValue(altKey, out actual))
                    return false;
            }

            var actualId = ExtractIdFromReference(actual);
            var matches = expectedValues.Any(ev =>
            {
                var expId = ExtractIdFromReference(ev);
                return string.Equals(actualId, expId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(actual, ev, StringComparison.OrdinalIgnoreCase);
            });
            if (!matches)
                return false;
        }

        return true;
    }

    private static Dictionary<string, List<string>> ParseQueryString(string query)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in query.Split('&'))
        {
            var idx = pair.IndexOf('=');
            if (idx <= 0) continue;
            var key = pair[..idx].Trim().ToLowerInvariant();
            var val = idx < pair.Length - 1 ? pair[(idx + 1)..].Trim() : "";
            if (string.IsNullOrEmpty(key)) continue;
            if (!result.TryGetValue(key, out var list))
            {
                list = [];
                result[key] = list;
            }
            var decoded = Uri.UnescapeDataString(val);
            if (!string.IsNullOrEmpty(decoded) && !list.Contains(decoded))
                list.Add(decoded);
        }
        return result;
    }

    private static string ExtractIdFromReference(string reference)
    {
        if (string.IsNullOrEmpty(reference)) return "";
        var parts = reference.Trim().Split('/');
        return parts.Length > 1 ? parts[^1].Trim() : reference.Trim();
    }
}
