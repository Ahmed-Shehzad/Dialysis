using System.Text.Json;
using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;
using Dialysis.BuildingBlocks.DataProtection.Erasure;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

/// <summary>
/// EF-backed <see cref="IErasureRequestStore"/>. Persists DSR Art. 17 requests on
/// <see cref="HieDbContext"/> under the <c>hie_documents</c> schema. The execution log
/// is stored as JSON so the per-module breakdown can grow without a schema change.
/// </summary>
public sealed class EfErasureRequestStore : IErasureRequestStore
{
    private readonly HieDbContext _db;
    /// <summary>
    /// EF-backed <see cref="IErasureRequestStore"/>. Persists DSR Art. 17 requests on
    /// <see cref="HieDbContext"/> under the <c>hie_documents</c> schema. The execution log
    /// is stored as JSON so the per-module breakdown can grow without a schema change.
    /// </summary>
    public EfErasureRequestStore(HieDbContext db) => _db = db;
    public async Task SaveAsync(ErasureRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await _db.ErasureRequests.FindAsync([request.Id], cancellationToken).ConfigureAwait(false);
        var row = ErasureRequestRow.FromDomain(request);
        if (existing is null)
        {
            _db.ErasureRequests.Add(row);
        }
        else
        {
            _db.Entry(existing).CurrentValues.SetValues(row);
        }
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ErasureRequest?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        var row = await _db.ErasureRequests.FindAsync([id], cancellationToken).ConfigureAwait(false);
        return row?.ToDomain();
    }

    public async Task<IReadOnlyList<ErasureRequest>> ListByStatusAsync(
        ErasureRequestStatus status, int take, CancellationToken cancellationToken)
    {
        var rows = await _db.ErasureRequests
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
/// Mutable EF mapping row for <see cref="ErasureRequest"/>. The domain object is a record
/// with read-only properties; EF Core 10 can read records but the auto-set scenarios are
/// cleaner with a parallel mutable class.
/// </summary>
public sealed class ErasureRequestRow
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public ErasureRequestStatus Status { get; set; }
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAtUtc { get; set; }
    public string? Reason { get; set; }
    public string? DecisionBy { get; set; }
    public DateTime? DecisionAtUtc { get; set; }
    public string? DecisionReason { get; set; }
    public string ExecutionLogJson { get; set; } = "[]";

    public ErasureRequest ToDomain()
    {
        var log = string.IsNullOrWhiteSpace(ExecutionLogJson)
            ? []
            : JsonSerializer.Deserialize<ErasureModuleResult[]>(ExecutionLogJson) ?? [];
        return new ErasureRequest(
            Id: Id,
            PatientId: PatientId,
            Status: Status,
            RequestedBy: RequestedBy,
            RequestedAtUtc: DateTime.SpecifyKind(RequestedAtUtc, DateTimeKind.Utc),
            Reason: Reason,
            DecisionBy: DecisionBy,
            DecisionAtUtc: DecisionAtUtc is null
                ? null
                : DateTime.SpecifyKind(DecisionAtUtc.Value, DateTimeKind.Utc),
            DecisionReason: DecisionReason,
            ExecutionLog: log);
    }

    public static ErasureRequestRow FromDomain(ErasureRequest request) => new()
    {
        Id = request.Id,
        PatientId = request.PatientId,
        Status = request.Status,
        RequestedBy = request.RequestedBy,
        RequestedAtUtc = request.RequestedAtUtc.UtcDateTime,
        Reason = request.Reason,
        DecisionBy = request.DecisionBy,
        DecisionAtUtc = request.DecisionAtUtc?.UtcDateTime,
        DecisionReason = request.DecisionReason,
        ExecutionLogJson = JsonSerializer.Serialize(request.ExecutionLog),
    };
}
