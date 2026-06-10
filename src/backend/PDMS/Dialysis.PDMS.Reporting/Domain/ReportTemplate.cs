using Dialysis.DomainDrivenDesign.Primitives;

namespace Dialysis.PDMS.Reporting.Domain;

/// <summary>
/// Operator-authored report template. One template per <see cref="ReportKind"/> may be the
/// active "Published" version at any time; older versions are kept as immutable history rows
/// so the operator can roll back any change. The body is Markdown with Mustache
/// (<c>{{patient.name}}</c>, etc.) bindings — the generator binds + renders to PDF via the
/// PDF building block.
/// </summary>
public sealed class ReportTemplate : AggregateRoot<Guid>
{
    private readonly List<ReportTemplateVersion> _versions = new();

    private ReportTemplate() { }

    public ReportTemplate(Guid id, string slug, ReportKind kind, string title, string? languageCode = null) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Slug = slug;
        Kind = kind;
        Title = title;
        LanguageCode = NormalizeLanguageCode(languageCode);
    }

    public string Slug { get; private set; } = null!;
    public ReportKind Kind { get; private set; }
    public string Title { get; private set; } = null!;

    /// <summary>
    /// BCP-47 language tag the template is authored in (e.g. <c>de</c>, <c>en-US</c>), or
    /// <c>null</c> for the operator-flagged default that applies when no language-specific
    /// template matches the patient's preferred language. Stored lower-cased so resolution is
    /// case-insensitive.
    /// </summary>
    public string? LanguageCode { get; private set; }

    public int? PublishedVersionNumber { get; private set; }
    public IReadOnlyCollection<ReportTemplateVersion> Versions => _versions.AsReadOnly();

    private static string? NormalizeLanguageCode(string? languageCode) =>
        string.IsNullOrWhiteSpace(languageCode) ? null : languageCode.Trim().ToLowerInvariant();

    /// <summary>Appends a new draft version. The new version becomes available for publish, not auto-published.</summary>
    public ReportTemplateVersion AppendVersion(
        string bodyMarkdown,
        string authoredBySub,
        DateTime authoredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bodyMarkdown);
        ArgumentException.ThrowIfNullOrWhiteSpace(authoredBySub);
        var versionNumber = (_versions.Count == 0 ? 0 : _versions.Max(v => v.VersionNumber)) + 1;
        var version = new ReportTemplateVersion(versionNumber, bodyMarkdown, authoredBySub, authoredAtUtc);
        _versions.Add(version);
        return version;
    }

    /// <summary>Switches the published version to <paramref name="versionNumber"/>. Rollbacks use the same operation.</summary>
    public void Publish(int versionNumber)
    {
        if (_versions.TrueForAll(v => v.VersionNumber != versionNumber))
            throw new InvalidOperationException($"Version {versionNumber} does not exist on template {Slug}.");
        PublishedVersionNumber = versionNumber;
    }

    /// <summary>Returns the currently published body, or <c>null</c> if no version has been published yet.</summary>
    public string? GetPublishedBody() => PublishedVersionNumber is null
        ? null
        : _versions.Find(v => v.VersionNumber == PublishedVersionNumber.Value)?.BodyMarkdown;
}

public sealed record ReportTemplateVersion
{
    public ReportTemplateVersion(int VersionNumber,
        string BodyMarkdown,
        string AuthoredBySub,
        DateTime AuthoredAtUtc)
    {
        this.VersionNumber = VersionNumber;
        this.BodyMarkdown = BodyMarkdown;
        this.AuthoredBySub = AuthoredBySub;
        this.AuthoredAtUtc = AuthoredAtUtc;
    }
    public int VersionNumber { get; init; }
    public string BodyMarkdown { get; init; }
    public string AuthoredBySub { get; init; }
    public DateTime AuthoredAtUtc { get; init; }
    public void Deconstruct(out int VersionNumber, out string BodyMarkdown, out string AuthoredBySub, out DateTime AuthoredAtUtc)
    {
        VersionNumber = this.VersionNumber;
        BodyMarkdown = this.BodyMarkdown;
        AuthoredBySub = this.AuthoredBySub;
        AuthoredAtUtc = this.AuthoredAtUtc;
    }
}

public enum ReportKind
{
    /// <summary>Patient discharge letter — one per completed session.</summary>
    DischargeLetter = 0,

    /// <summary>Shift report — every session in a chair / shift window.</summary>
    ShiftReport = 1,

    /// <summary>Billing summary — feeds the EDI 837 generator with line items.</summary>
    BillingDocument = 2,
}
