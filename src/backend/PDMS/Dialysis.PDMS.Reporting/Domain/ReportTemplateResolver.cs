namespace Dialysis.PDMS.Reporting.Domain;

/// <summary>
/// Language-aware resolution of the active <see cref="ReportTemplate"/> for a report kind.
/// A deployment can author any number of locale-specific templates of the same kind plus one
/// language-neutral default; the resolver picks the best match for the patient's preferred
/// language without further code changes.
///
/// Match order (first hit wins):
/// <list type="number">
///   <item>Exact published template for <c>(kind, preferredLanguage)</c> — e.g. <c>de-DE</c>.</item>
///   <item>Published template for the primary subtag <c>(kind, "de")</c> when the preferred
///         language carried a region (<c>de-DE</c> → <c>de</c>).</item>
///   <item>The language-neutral published default <c>(kind, LanguageCode == null)</c>.</item>
/// </list>
/// Only published templates are eligible; an unpublished draft never resolves. Returns
/// <c>null</c> when nothing matches, so the generator can fall back to its built-in body.
/// </summary>
public static class ReportTemplateResolver
{
    public static ReportTemplate? Resolve(
        IEnumerable<ReportTemplate> candidates,
        ReportKind kind,
        string? preferredLanguageCode)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var published = candidates
            .Where(t => t.Kind == kind && t.PublishedVersionNumber is not null)
            .ToList();
        if (published.Count == 0)
            return null;

        var normalized = string.IsNullOrWhiteSpace(preferredLanguageCode)
            ? null
            : preferredLanguageCode.Trim().ToLowerInvariant();

        if (normalized is not null)
        {
            // 1. exact (kind, language) match.
            var exact = published.Find(t => t.LanguageCode == normalized);
            if (exact is not null)
                return exact;

            // 2. primary subtag match (de-de → de).
            var dash = normalized.IndexOf('-', StringComparison.Ordinal);
            if (dash > 0)
            {
                var primary = normalized[..dash];
                var bySubtag = published.Find(t => t.LanguageCode == primary);
                if (bySubtag is not null)
                    return bySubtag;
            }
        }

        // 3. language-neutral default.
        return published.Find(t => t.LanguageCode is null);
    }
}
