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
public sealed record RetentionPolicyRow
{
    /// <summary>Operator-visible row.</summary>
    public RetentionPolicyRow(Guid Id, string Kind, int RetentionDays, DateTime UpdatedAtUtc, string UpdatedBy)
    {
        this.Id = Id;
        this.Kind = Kind;
        this.RetentionDays = RetentionDays;
        this.UpdatedAtUtc = UpdatedAtUtc;
        this.UpdatedBy = UpdatedBy;
    }
    public Guid Id { get; init; }
    public string Kind { get; init; }
    public int RetentionDays { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public string UpdatedBy { get; init; }
    public void Deconstruct(out Guid Id, out string Kind, out int RetentionDays, out DateTime UpdatedAtUtc, out string UpdatedBy)
    {
        Id = this.Id;
        Kind = this.Kind;
        RetentionDays = this.RetentionDays;
        UpdatedAtUtc = this.UpdatedAtUtc;
        UpdatedBy = this.UpdatedBy;
    }
}

// -------- List --------

public sealed record ListRetentionPoliciesQuery : IQuery<IReadOnlyList<RetentionPolicyRow>>, IPermissionedCommand
{
    public string RequiredPermission => HiePermissions.DocumentsRetentionView;
}

public sealed class ListRetentionPoliciesQueryHandler : IQueryHandler<ListRetentionPoliciesQuery, IReadOnlyList<RetentionPolicyRow>>
{
    private readonly IDocumentRetentionPolicyRepository _repository;
    public ListRetentionPoliciesQueryHandler(IDocumentRetentionPolicyRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<RetentionPolicyRow>> HandleAsync(
        ListRetentionPoliciesQuery request, CancellationToken cancellationToken)
    {
        var rows = await _repository.ListAsync(cancellationToken).ConfigureAwait(false);
        return [.. rows.Select(p => new RetentionPolicyRow(p.Id, p.Kind, p.RetentionDays, p.UpdatedAtUtc, p.UpdatedBy))];
    }
}

// -------- Upsert --------

public sealed record UpsertRetentionPolicyCommand : ICommand<Guid>, IPermissionedCommand
{
    public UpsertRetentionPolicyCommand(string Kind, int RetentionDays, string UpdatedBy)
    {
        this.Kind = Kind;
        this.RetentionDays = RetentionDays;
        this.UpdatedBy = UpdatedBy;
    }
    public string RequiredPermission => HiePermissions.DocumentsRetentionAdminister;
    public string Kind { get; init; }
    public int RetentionDays { get; init; }
    public string UpdatedBy { get; init; }
    public void Deconstruct(out string Kind, out int RetentionDays, out string UpdatedBy)
    {
        Kind = this.Kind;
        RetentionDays = this.RetentionDays;
        UpdatedBy = this.UpdatedBy;
    }
}

public sealed class UpsertRetentionPolicyCommandHandler : ICommandHandler<UpsertRetentionPolicyCommand, Guid>
{
    private readonly IDocumentRetentionPolicyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _clock;
    public UpsertRetentionPolicyCommandHandler(IDocumentRetentionPolicyRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }
    public async Task<Guid> HandleAsync(UpsertRetentionPolicyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = _clock.GetUtcNow().UtcDateTime;
        var existing = await _repository.FindByKindAsync(request.Kind, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            var created = new DocumentRetentionPolicy(
                Guid.CreateVersion7(), request.Kind, request.RetentionDays, now, request.UpdatedBy);
            _repository.Add(created);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return created.Id;
        }
        existing.Revise(request.RetentionDays, now, request.UpdatedBy);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return existing.Id;
    }
}

// -------- Delete --------

public sealed record DeleteRetentionPolicyCommand : ICommand, IPermissionedCommand
{
    public DeleteRetentionPolicyCommand(string Kind) => this.Kind = Kind;
    public string RequiredPermission => HiePermissions.DocumentsRetentionAdminister;
    public string Kind { get; init; }
    public void Deconstruct(out string Kind) => Kind = this.Kind;
}

public sealed class DeleteRetentionPolicyCommandHandler : ICommandHandler<DeleteRetentionPolicyCommand>
{
    private readonly IDocumentRetentionPolicyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteRetentionPolicyCommandHandler(IDocumentRetentionPolicyRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    public async Task<Unit> HandleAsync(DeleteRetentionPolicyCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.FindByKindAsync(request.Kind, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return Unit.Value;
        _repository.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
