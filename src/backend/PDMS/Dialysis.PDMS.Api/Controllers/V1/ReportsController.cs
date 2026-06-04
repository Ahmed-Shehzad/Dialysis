using Asp.Versioning;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Reporting.Domain;
using Dialysis.PDMS.Reporting.Generators;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

/// <summary>
/// Read-only HTTP surface for the SessionReport + ReportTemplate aggregates.
/// <list type="bullet">
///   <item><c>GET /sessions/{id}/reports</c> — list every generated report for one session.</item>
///   <item><c>GET /reports/{id}</c> — fetch one report's metadata.</item>
///   <item><c>GET /reports/{id}/content</c> — download the rendered PDF bytes.</item>
///   <item><c>GET /reporting/templates</c> — list operator-authored templates.</item>
///   <item><c>POST /reporting/templates</c> — append a new draft version.</item>
///   <item><c>POST /reporting/templates/{slug}/publish</c> — publish (or rollback) a version.</item>
/// </list>
/// </summary>
[ApiController]
[ApiVersion("1.0")]
public sealed class ReportsController : ControllerBase
{
    private readonly IPdmsRepository<SessionReport, Guid> _reports;
    private readonly IPdmsRepository<ReportTemplate, Guid> _templates;
    private readonly IReportBlobStore _blobs;
    private readonly TimeProvider _clock;
    /// <summary>
    /// Read-only HTTP surface for the SessionReport + ReportTemplate aggregates.
    /// <list type="bullet">
    ///   <item><c>GET /sessions/{id}/reports</c> — list every generated report for one session.</item>
    ///   <item><c>GET /reports/{id}</c> — fetch one report's metadata.</item>
    ///   <item><c>GET /reports/{id}/content</c> — download the rendered PDF bytes.</item>
    ///   <item><c>GET /reporting/templates</c> — list operator-authored templates.</item>
    ///   <item><c>POST /reporting/templates</c> — append a new draft version.</item>
    ///   <item><c>POST /reporting/templates/{slug}/publish</c> — publish (or rollback) a version.</item>
    /// </list>
    /// </summary>
    public ReportsController(IPdmsRepository<SessionReport, Guid> reports,
        IPdmsRepository<ReportTemplate, Guid> templates,
        IReportBlobStore blobs,
        TimeProvider clock)
    {
        _reports = reports;
        _templates = templates;
        _blobs = blobs;
        _clock = clock;
    }
    [HttpGet("api/v{version:apiVersion}/sessions/{sessionId:guid}/reports")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListForSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var all = await _reports.ListAsync(null, cancellationToken).ConfigureAwait(false);
        var rows = all
            .Where(r => r.SessionId == sessionId)
            .OrderByDescending(r => r.GeneratedAtUtc ?? DateTime.MinValue)
            .Select(SessionReportDto.From)
            .ToArray();
        return Ok(rows);
    }

    [HttpGet("api/v{version:apiVersion}/reports/{reportId:guid}")]
    [ProducesResponseType(typeof(SessionReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAsync(Guid reportId, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByIdAsync(reportId, cancellationToken).ConfigureAwait(false);
        if (report is null)
            return NotFound();
        return Ok(SessionReportDto.From(report));
    }

    [HttpGet("api/v{version:apiVersion}/reports/{reportId:guid}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAsync(Guid reportId, CancellationToken cancellationToken)
    {
        var report = await _reports.GetByIdAsync(reportId, cancellationToken).ConfigureAwait(false);
        if (report is null || report.StorageRef is null)
            return NotFound();
        var bytes = await _blobs.ReadAsync(report.StorageRef, cancellationToken).ConfigureAwait(false);
        if (bytes is null)
            return NotFound();
        return File(bytes, report.Format, $"{report.Kind.ToString().ToLowerInvariant()}-{report.SessionId:N}.pdf");
    }

    [HttpGet("api/v{version:apiVersion}/reporting/templates")]
    [ProducesResponseType(typeof(IReadOnlyList<ReportTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTemplatesAsync(
        [FromQuery] string? kind = null,
        CancellationToken cancellationToken = default)
    {
        var all = await _templates.ListAsync(null, cancellationToken).ConfigureAwait(false);
        IEnumerable<ReportTemplate> filtered = all;
        if (kind is not null && Enum.TryParse<ReportKind>(kind, ignoreCase: true, out var parsed))
            filtered = filtered.Where(t => t.Kind == parsed);
        return Ok(filtered.Select(ReportTemplateDto.From).ToArray());
    }

    [HttpPost("api/v{version:apiVersion}/reporting/templates")]
    [ProducesResponseType(typeof(ReportTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AppendVersionAsync(
        [FromBody] AppendTemplateVersionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.TryParse<ReportKind>(request.Kind, ignoreCase: true, out var kind))
            return BadRequest($"Unknown report kind '{request.Kind}'.");

        var existing = await FindTemplateBySlugAsync(request.Slug, request.LanguageCode, cancellationToken).ConfigureAwait(false);
        var template = existing ?? new ReportTemplate(
            id: Guid.CreateVersion7(),
            slug: request.Slug,
            kind: kind,
            title: request.Title,
            languageCode: request.LanguageCode);
        template.AppendVersion(request.BodyMarkdown, request.AuthoredBySub, _clock.GetUtcNow().UtcDateTime);
        if (existing is null)
            await _templates.AddAsync(template, cancellationToken).ConfigureAwait(false);
        else
            _templates.Update(template);
        return CreatedAtAction(nameof(ListTemplatesAsync), null, ReportTemplateDto.From(template));
    }

    [HttpPost("api/v{version:apiVersion}/reporting/templates/{slug}/publish")]
    [ProducesResponseType(typeof(ReportTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PublishAsync(
        string slug,
        [FromBody] PublishTemplateRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var template = await FindTemplateBySlugAsync(slug, request.LanguageCode, cancellationToken).ConfigureAwait(false);
        if (template is null)
            return NotFound();
        try
        { template.Publish(request.VersionNumber); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        _templates.Update(template);
        return Ok(ReportTemplateDto.From(template));
    }

    // Templates are keyed by (slug, languageCode): the language-neutral default carries a null
    // language, locale-specific siblings carry their BCP-47 tag. Matching is case-insensitive.
    private async Task<ReportTemplate?> FindTemplateBySlugAsync(
        string slug, string? languageCode, CancellationToken cancellationToken)
    {
        var normalized = string.IsNullOrWhiteSpace(languageCode) ? null : languageCode.Trim().ToLowerInvariant();
        var all = await _templates.ListAsync(null, cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(t =>
            string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase)
            && t.LanguageCode == normalized);
    }
}

public sealed record AppendTemplateVersionRequest
{
    public AppendTemplateVersionRequest(string Slug,
        string Kind,
        string Title,
        string BodyMarkdown,
        string AuthoredBySub,
        string? LanguageCode = null)
    {
        this.Slug = Slug;
        this.Kind = Kind;
        this.Title = Title;
        this.BodyMarkdown = BodyMarkdown;
        this.AuthoredBySub = AuthoredBySub;
        this.LanguageCode = LanguageCode;
    }
    public string Slug { get; init; }
    public string Kind { get; init; }
    public string Title { get; init; }
    public string BodyMarkdown { get; init; }
    public string AuthoredBySub { get; init; }
    public string? LanguageCode { get; init; }
    public void Deconstruct(out string Slug, out string Kind, out string Title, out string BodyMarkdown, out string AuthoredBySub, out string? LanguageCode)
    {
        Slug = this.Slug;
        Kind = this.Kind;
        Title = this.Title;
        BodyMarkdown = this.BodyMarkdown;
        AuthoredBySub = this.AuthoredBySub;
        LanguageCode = this.LanguageCode;
    }
}

public sealed record PublishTemplateRequest
{
    public PublishTemplateRequest(int VersionNumber, string? LanguageCode = null)
    {
        this.VersionNumber = VersionNumber;
        this.LanguageCode = LanguageCode;
    }
    public int VersionNumber { get; init; }
    public string? LanguageCode { get; init; }
    public void Deconstruct(out int VersionNumber, out string? LanguageCode)
    {
        VersionNumber = this.VersionNumber;
        LanguageCode = this.LanguageCode;
    }
}

public sealed record SessionReportDto
{
    public SessionReportDto(Guid Id,
        Guid SessionId,
        Guid PatientId,
        string Kind,
        string Status,
        string Format,
        string? ContentHash,
        string? StorageRef,
        DateTime? GeneratedAtUtc,
        DateTime? DeliveredAtUtc,
        string? FailureReason)
    {
        this.Id = Id;
        this.SessionId = SessionId;
        this.PatientId = PatientId;
        this.Kind = Kind;
        this.Status = Status;
        this.Format = Format;
        this.ContentHash = ContentHash;
        this.StorageRef = StorageRef;
        this.GeneratedAtUtc = GeneratedAtUtc;
        this.DeliveredAtUtc = DeliveredAtUtc;
        this.FailureReason = FailureReason;
    }
    public static SessionReportDto From(SessionReport r) => new(
        Id: r.Id,
        SessionId: r.SessionId,
        PatientId: r.PatientId,
        Kind: r.Kind.ToString(),
        Status: r.Status.ToString(),
        Format: r.Format,
        ContentHash: r.ContentHash,
        StorageRef: r.StorageRef,
        GeneratedAtUtc: r.GeneratedAtUtc,
        DeliveredAtUtc: r.DeliveredAtUtc,
        FailureReason: r.FailureReason);
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public Guid PatientId { get; init; }
    public string Kind { get; init; }
    public string Status { get; init; }
    public string Format { get; init; }
    public string? ContentHash { get; init; }
    public string? StorageRef { get; init; }
    public DateTime? GeneratedAtUtc { get; init; }
    public DateTime? DeliveredAtUtc { get; init; }
    public string? FailureReason { get; init; }
    public void Deconstruct(out Guid Id, out Guid SessionId, out Guid PatientId, out string Kind, out string Status, out string Format, out string? ContentHash, out string? StorageRef, out DateTime? GeneratedAtUtc, out DateTime? DeliveredAtUtc, out string? FailureReason)
    {
        Id = this.Id;
        SessionId = this.SessionId;
        PatientId = this.PatientId;
        Kind = this.Kind;
        Status = this.Status;
        Format = this.Format;
        ContentHash = this.ContentHash;
        StorageRef = this.StorageRef;
        GeneratedAtUtc = this.GeneratedAtUtc;
        DeliveredAtUtc = this.DeliveredAtUtc;
        FailureReason = this.FailureReason;
    }
}

public sealed record ReportTemplateDto
{
    public ReportTemplateDto(Guid Id,
        string Slug,
        string Kind,
        string Title,
        string? LanguageCode,
        int? PublishedVersionNumber,
        IReadOnlyList<ReportTemplateVersionDto> Versions)
    {
        this.Id = Id;
        this.Slug = Slug;
        this.Kind = Kind;
        this.Title = Title;
        this.LanguageCode = LanguageCode;
        this.PublishedVersionNumber = PublishedVersionNumber;
        this.Versions = Versions;
    }
    public static ReportTemplateDto From(ReportTemplate t) => new(
        Id: t.Id,
        Slug: t.Slug,
        Kind: t.Kind.ToString(),
        Title: t.Title,
        LanguageCode: t.LanguageCode,
        PublishedVersionNumber: t.PublishedVersionNumber,
        Versions: [.. t.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new ReportTemplateVersionDto(
                v.VersionNumber, v.BodyMarkdown, v.AuthoredBySub, v.AuthoredAtUtc))]);
    public Guid Id { get; init; }
    public string Slug { get; init; }
    public string Kind { get; init; }
    public string Title { get; init; }
    public string? LanguageCode { get; init; }
    public int? PublishedVersionNumber { get; init; }
    public IReadOnlyList<ReportTemplateVersionDto> Versions { get; init; }
    public void Deconstruct(out Guid Id, out string Slug, out string Kind, out string Title, out string? LanguageCode, out int? PublishedVersionNumber, out IReadOnlyList<ReportTemplateVersionDto> Versions)
    {
        Id = this.Id;
        Slug = this.Slug;
        Kind = this.Kind;
        Title = this.Title;
        LanguageCode = this.LanguageCode;
        PublishedVersionNumber = this.PublishedVersionNumber;
        Versions = this.Versions;
    }
}

public sealed record ReportTemplateVersionDto
{
    public ReportTemplateVersionDto(int VersionNumber,
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
