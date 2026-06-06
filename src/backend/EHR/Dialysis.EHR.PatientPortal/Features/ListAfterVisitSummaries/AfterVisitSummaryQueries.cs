using Dialysis.CQRS.Queries;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.ListAfterVisitSummaries;

/// <summary>An education / web-resource link projected for the portal.</summary>
public sealed record AvsResourceLinkView(string Label, string Url);

/// <summary>A patient-friendly after-visit summary projected for the portal.</summary>
public sealed record AfterVisitSummaryView(
    Guid Id,
    Guid PatientId,
    DateTime VisitDateUtc,
    string Narrative,
    string Status,
    DateTime? PublishedAtUtc,
    IReadOnlyList<string> Instructions,
    IReadOnlyList<string> FollowUps,
    IReadOnlyList<AvsResourceLinkView> ResourceLinks);

internal static class AfterVisitSummaryMapping
{
    public static AfterVisitSummaryView ToView(this AfterVisitSummary s) =>
        new(s.Id, s.PatientId, s.VisitDateUtc, s.Narrative, s.Status.ToString(), s.PublishedAtUtc,
            [.. s.Instructions.Select(i => i.Text)],
            [.. s.FollowUps.Select(f => f.Text)],
            [.. s.ResourceLinks.Select(l => new AvsResourceLinkView(l.Label, l.Url))]);
}

/// <summary>A patient's published after-visit summaries (portal list).</summary>
public sealed record ListMyAfterVisitSummariesQuery(Guid PatientId)
    : IQuery<IReadOnlyList<AfterVisitSummaryView>>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalRead;
}

public sealed class ListMyAfterVisitSummariesQueryHandler
    : IQueryHandler<ListMyAfterVisitSummariesQuery, IReadOnlyList<AfterVisitSummaryView>>
{
    private readonly IAfterVisitSummaryRepository _summaries;
    public ListMyAfterVisitSummariesQueryHandler(IAfterVisitSummaryRepository summaries) => _summaries = summaries;

    public async Task<IReadOnlyList<AfterVisitSummaryView>> HandleAsync(
        ListMyAfterVisitSummariesQuery request, CancellationToken cancellationToken)
    {
        var rows = await _summaries.ListByPatientAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        // Patients only see what's been published.
        return [.. rows.Where(s => s.Status == AfterVisitSummaryStatus.Published).Select(s => s.ToView())];
    }
}

/// <summary>One after-visit summary by id (portal detail view).</summary>
public sealed record GetAfterVisitSummaryByIdQuery(Guid SummaryId)
    : IQuery<AfterVisitSummaryView?>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalRead;
}

public sealed class GetAfterVisitSummaryByIdQueryHandler
    : IQueryHandler<GetAfterVisitSummaryByIdQuery, AfterVisitSummaryView?>
{
    private readonly IAfterVisitSummaryRepository _summaries;
    public GetAfterVisitSummaryByIdQueryHandler(IAfterVisitSummaryRepository summaries) => _summaries = summaries;

    public async Task<AfterVisitSummaryView?> HandleAsync(
        GetAfterVisitSummaryByIdQuery request, CancellationToken cancellationToken)
    {
        var summary = await _summaries.GetAsync(request.SummaryId, cancellationToken).ConfigureAwait(false);
        return summary is null || summary.Status != AfterVisitSummaryStatus.Published ? null : summary.ToView();
    }
}
