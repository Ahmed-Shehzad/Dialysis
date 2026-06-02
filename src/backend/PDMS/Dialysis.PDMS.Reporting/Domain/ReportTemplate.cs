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

    public ReportTemplate(Guid id, string slug, ReportKind kind, string title) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Slug = slug;
        Kind = kind;
        Title = title;
    }

    public string Slug { get; private set; } = null!;
    public ReportKind Kind { get; private set; }
    public string Title { get; private set; } = null!;
    public int? PublishedVersionNumber { get; private set; }
    public IReadOnlyCollection<ReportTemplateVersion> Versions => _versions.AsReadOnly();

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
        if (_versions.All(v => v.VersionNumber != versionNumber))
            throw new InvalidOperationException($"Version {versionNumber} does not exist on template {Slug}.");
        PublishedVersionNumber = versionNumber;
    }

    /// <summary>Returns the currently published body, or <c>null</c> if no version has been published yet.</summary>
    public string? GetPublishedBody() => PublishedVersionNumber is null
        ? null
        : _versions.FirstOrDefault(v => v.VersionNumber == PublishedVersionNumber.Value)?.BodyMarkdown;
}

public sealed record ReportTemplateVersion(
    int VersionNumber,
    string BodyMarkdown,
    string AuthoredBySub,
    DateTime AuthoredAtUtc);

public enum ReportKind
{
    /// <summary>Patient discharge letter — one per completed session.</summary>
    DischargeLetter = 0,

    /// <summary>Shift report — every session in a chair / shift window.</summary>
    ShiftReport = 1,

    /// <summary>Billing summary — feeds the EDI 837 generator with line items.</summary>
    BillingDocument = 2,
}
