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
public sealed class ReportsController(
    IPdmsRepository<SessionReport, Guid> reports,
    IPdmsRepository<ReportTemplate, Guid> templates,
    IReportBlobStore blobs,
    TimeProvider clock) : ControllerBase
{
    [HttpGet("api/v{version:apiVersion}/sessions/{sessionId:guid}/reports")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListForSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var all = await reports.ListAsync(null, cancellationToken).ConfigureAwait(false);
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
        var report = await reports.GetByIdAsync(reportId, cancellationToken).ConfigureAwait(false);
        if (report is null) return NotFound();
        return Ok(SessionReportDto.From(report));
    }

    [HttpGet("api/v{version:apiVersion}/reports/{reportId:guid}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAsync(Guid reportId, CancellationToken cancellationToken)
    {
        var report = await reports.GetByIdAsync(reportId, cancellationToken).ConfigureAwait(false);
        if (report is null || report.StorageRef is null) return NotFound();
        var bytes = await blobs.ReadAsync(report.StorageRef, cancellationToken).ConfigureAwait(false);
        if (bytes is null) return NotFound();
        return File(bytes, report.Format, $"{report.Kind.ToString().ToLowerInvariant()}-{report.SessionId:N}.pdf");
    }

    [HttpGet("api/v{version:apiVersion}/reporting/templates")]
    [ProducesResponseType(typeof(IReadOnlyList<ReportTemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTemplatesAsync(
        [FromQuery] string? kind = null,
        CancellationToken cancellationToken = default)
    {
        var all = await templates.ListAsync(null, cancellationToken).ConfigureAwait(false);
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

        var existing = await FindTemplateBySlugAsync(request.Slug, cancellationToken).ConfigureAwait(false);
        var template = existing ?? new ReportTemplate(
            id: Guid.CreateVersion7(),
            slug: request.Slug,
            kind: kind,
            title: request.Title);
        template.AppendVersion(request.BodyMarkdown, request.AuthoredBySub, clock.GetUtcNow().UtcDateTime);
        if (existing is null) await templates.AddAsync(template, cancellationToken).ConfigureAwait(false);
        else templates.Update(template);
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
        var template = await FindTemplateBySlugAsync(slug, cancellationToken).ConfigureAwait(false);
        if (template is null) return NotFound();
        try { template.Publish(request.VersionNumber); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        templates.Update(template);
        return Ok(ReportTemplateDto.From(template));
    }

    private async Task<ReportTemplate?> FindTemplateBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var all = await templates.ListAsync(null, cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(t => string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record AppendTemplateVersionRequest(
    string Slug,
    string Kind,
    string Title,
    string BodyMarkdown,
    string AuthoredBySub);

public sealed record PublishTemplateRequest(int VersionNumber);

public sealed record SessionReportDto(
    Guid Id,
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
}

public sealed record ReportTemplateDto(
    Guid Id,
    string Slug,
    string Kind,
    string Title,
    int? PublishedVersionNumber,
    IReadOnlyList<ReportTemplateVersionDto> Versions)
{
    public static ReportTemplateDto From(ReportTemplate t) => new(
        Id: t.Id,
        Slug: t.Slug,
        Kind: t.Kind.ToString(),
        Title: t.Title,
        PublishedVersionNumber: t.PublishedVersionNumber,
        Versions: t.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new ReportTemplateVersionDto(
                v.VersionNumber, v.BodyMarkdown, v.AuthoredBySub, v.AuthoredAtUtc))
            .ToArray());
}

public sealed record ReportTemplateVersionDto(
    int VersionNumber,
    string BodyMarkdown,
    string AuthoredBySub,
    DateTime AuthoredAtUtc);
