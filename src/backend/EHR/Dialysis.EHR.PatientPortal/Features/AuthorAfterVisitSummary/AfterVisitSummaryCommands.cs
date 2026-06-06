using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.Security;
using Dialysis.EHR.PatientPortal.Domain;
using Dialysis.EHR.PatientPortal.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.EHR.PatientPortal.Features.AuthorAfterVisitSummary;

/// <summary>Which kind of line an <see cref="AddAfterVisitSummaryLineCommand"/> appends.</summary>
public enum AfterVisitLineKind
{
    Instruction = 1,
    FollowUp = 2,
    ResourceLink = 3,
}

/// <summary>Clinician starts a draft after-visit summary for a patient's encounter.</summary>
public sealed record CreateAfterVisitSummaryCommand(
    Guid PatientId,
    Guid EncounterRef,
    DateTime VisitDateUtc,
    Guid AuthoringProviderId,
    string Narrative) : ICommand<Guid>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalAvsAuthor;
}

public sealed class CreateAfterVisitSummaryCommandHandler : ICommandHandler<CreateAfterVisitSummaryCommand, Guid>
{
    private readonly IAfterVisitSummaryRepository _summaries;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAfterVisitSummaryCommandHandler(IAfterVisitSummaryRepository summaries, IUnitOfWork unitOfWork)
    {
        _summaries = summaries;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(CreateAfterVisitSummaryCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var summary = AfterVisitSummary.CreateDraft(
            id, request.PatientId, request.EncounterRef, request.VisitDateUtc, request.AuthoringProviderId, request.Narrative);
        _summaries.Add(summary);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}

/// <summary>
/// Appends a line to a draft summary. For <see cref="AfterVisitLineKind.ResourceLink"/>, <c>Text</c> is
/// the label and <c>Url</c> the link; otherwise <c>Text</c> is the instruction / follow-up text.
/// </summary>
public sealed record AddAfterVisitSummaryLineCommand(
    Guid SummaryId, AfterVisitLineKind Kind, string Text, string? Url) : ICommand<Guid>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalAvsAuthor;
}

public sealed class AddAfterVisitSummaryLineCommandHandler : ICommandHandler<AddAfterVisitSummaryLineCommand, Guid>
{
    private readonly IAfterVisitSummaryRepository _summaries;
    private readonly IUnitOfWork _unitOfWork;

    public AddAfterVisitSummaryLineCommandHandler(IAfterVisitSummaryRepository summaries, IUnitOfWork unitOfWork)
    {
        _summaries = summaries;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(AddAfterVisitSummaryLineCommand request, CancellationToken cancellationToken)
    {
        var summary = await _summaries.GetAsync(request.SummaryId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"After-visit summary {request.SummaryId} not found.");

        var lineId = Guid.CreateVersion7();
        _ = request.Kind switch
        {
            AfterVisitLineKind.Instruction => summary.AddInstruction(lineId, request.Text),
            AfterVisitLineKind.FollowUp => summary.AddFollowUp(lineId, request.Text),
            AfterVisitLineKind.ResourceLink => summary.AddResourceLink(lineId, request.Text, request.Url ?? string.Empty),
            _ => throw new ArgumentOutOfRangeException(nameof(request)),
        };
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return lineId;
    }
}

/// <summary>Publishes a draft summary to the patient portal.</summary>
public sealed record PublishAfterVisitSummaryCommand(Guid SummaryId) : ICommand<Unit>, IPermissionedCommand
{
    /// <inheritdoc />
    public string RequiredPermission => EhrPermissions.PortalAvsAuthor;
}

public sealed class PublishAfterVisitSummaryCommandHandler : ICommandHandler<PublishAfterVisitSummaryCommand, Unit>
{
    private readonly IAfterVisitSummaryRepository _summaries;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public PublishAfterVisitSummaryCommandHandler(
        IAfterVisitSummaryRepository summaries, IUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _summaries = summaries;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Unit> HandleAsync(PublishAfterVisitSummaryCommand request, CancellationToken cancellationToken)
    {
        var summary = await _summaries.GetAsync(request.SummaryId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"After-visit summary {request.SummaryId} not found.");
        summary.Publish(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
