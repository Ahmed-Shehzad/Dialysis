using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;
using Dialysis.BuildingBlocks.DataProtection.Restriction;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

/// <summary>
/// EF-backed <see cref="IRestrictionRequestStore"/>. Persists DSR Art. 18 requests on
/// <see cref="HieDbContext"/> under the <c>hie_documents</c> schema, alongside the erasure
/// audit trail, so a restriction survives host restarts for regulator review.
/// </summary>
public sealed class EfRestrictionRequestStore : IRestrictionRequestStore
{
    private readonly HieDbContext _db;
    /// <summary>
    /// EF-backed <see cref="IRestrictionRequestStore"/>. Persists DSR Art. 18 requests on
    /// <see cref="HieDbContext"/> under the <c>hie_documents</c> schema.
    /// </summary>
    public EfRestrictionRequestStore(HieDbContext db) => _db = db;

    public async Task SaveAsync(RestrictionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await _db.RestrictionRequests.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);
        var row = RestrictionRequestRow.FromDomain(request);
        if (existing is null)
        {
            _db.RestrictionRequests.Add(row);
        }
        else
        {
            _db.Entry(existing).CurrentValues.SetValues(row);
        }
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<RestrictionRequest?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _db.RestrictionRequests.FindAsync([id], cancellationToken).ConfigureAwait(false);
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<RestrictionRequest>> ListByStatusAsync(
        RestrictionRequestStatus status, int take, CancellationToken cancellationToken)
    {
        var rows = await _db.RestrictionRequests
            .AsNoTracking()
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.RequestedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. rows.Select(r => r.ToDomain())];
    }
}

/// <summary>
/// Mutable EF mapping row for <see cref="RestrictionRequest"/>. The domain object is a record
/// with read-only properties; a parallel mutable class keeps EF's change-tracking clean.
/// </summary>
public sealed class RestrictionRequestRow
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public RestrictionRequestStatus Status { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public string? Reason { get; set; }
    public string? LiftedBy { get; set; }
    public DateTime? LiftedAtUtc { get; set; }
    public string? LiftReason { get; set; }

    public RestrictionRequest ToDomain() =>
        new(
            Id: Id,
            PatientId: PatientId,
            Status: Status,
            RequestedBy: RequestedBy,
            RequestedAtUtc: DateTime.SpecifyKind(RequestedAtUtc, DateTimeKind.Utc),
            Reason: Reason,
            LiftedBy: LiftedBy,
            LiftedAtUtc: LiftedAtUtc is null
                ? null
                : DateTime.SpecifyKind(LiftedAtUtc.Value, DateTimeKind.Utc),
            LiftReason: LiftReason);

    public static RestrictionRequestRow FromDomain(RestrictionRequest request) => new()
    {
        Id = request.Id,
        PatientId = request.PatientId,
        Status = request.Status,
        RequestedBy = request.RequestedBy,
        RequestedAtUtc = request.RequestedAtUtc.UtcDateTime,
        Reason = request.Reason,
        LiftedBy = request.LiftedBy,
        LiftedAtUtc = request.LiftedAtUtc?.UtcDateTime,
        LiftReason = request.LiftReason,
    };
}
