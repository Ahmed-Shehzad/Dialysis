using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.CQRS.Queries;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIE.Contracts.Security;
using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Ports;
using Dialysis.Module.Contracts.Authorization;

namespace Dialysis.HIE.Documents.Features.Retention;

/// <summary>Operator-visible row.</summary>
public sealed record RetentionPolicyRow(
    Guid Id, string Kind, int RetentionDays, DateTime UpdatedAtUtc, string UpdatedBy);

// -------- List --------

public sealed record ListRetentionPoliciesQuery : IQuery<IReadOnlyList<RetentionPolicyRow>>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.DocumentsRetentionView;
}

public sealed class ListRetentionPoliciesQueryHandler(IDocumentRetentionPolicyRepository repository)
    : IQueryHandler<ListRetentionPoliciesQuery, IReadOnlyList<RetentionPolicyRow>>
{
    public async Task<IReadOnlyList<RetentionPolicyRow>> HandleAsync(
        ListRetentionPoliciesQuery request, CancellationToken cancellationToken)
    {
        var rows = await repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(p => new RetentionPolicyRow(p.Id, p.Kind, p.RetentionDays, p.UpdatedAtUtc, p.UpdatedBy))];
    }
}

// -------- Upsert --------

public sealed record UpsertRetentionPolicyCommand(string Kind, int RetentionDays, string UpdatedBy)
    : ICommand<Guid>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.DocumentsRetentionAdminister;
}

public sealed class UpsertRetentionPolicyCommandHandler(
    IDocumentRetentionPolicyRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : ICommandHandler<UpsertRetentionPolicyCommand, Guid>
{
    public async Task<Guid> HandleAsync(UpsertRetentionPolicyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = clock.GetUtcNow().UtcDateTime;
        var existing = await repository.FindByKindAsync(request.Kind, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            var created = new DocumentRetentionPolicy(
                Guid.CreateVersion7(), request.Kind, request.RetentionDays, now, request.UpdatedBy);
            repository.Add(created);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return created.Id;
        }
        existing.Revise(request.RetentionDays, now, request.UpdatedBy);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return existing.Id;
    }
}

// -------- Delete --------

public sealed record DeleteRetentionPolicyCommand(string Kind) : ICommand, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.DocumentsRetentionAdminister;
}

public sealed class DeleteRetentionPolicyCommandHandler(
    IDocumentRetentionPolicyRepository repository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteRetentionPolicyCommand>
{
    public async Task<Unit> HandleAsync(DeleteRetentionPolicyCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindByKindAsync(request.Kind, cancellationToken).ConfigureAwait(false);
        if (existing is null) return Unit.Value;
        repository.Remove(existing);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
