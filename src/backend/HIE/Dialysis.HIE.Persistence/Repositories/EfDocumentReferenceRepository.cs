using Dialysis.HIE.Documents.Domain;
using Dialysis.HIE.Documents.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfDocumentReferenceRepository : IDocumentReferenceRepository
{
    private readonly HieDbContext _db;
    public EfDocumentReferenceRepository(HieDbContext db) => _db = db;
    public void Add(DocumentReference document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _db.DocumentReferences.Add(document);
    }

    public Task<DocumentReference?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        _db.DocumentReferences
            .Include(d => d.Signatures)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<IReadOnlyList<DocumentReference>> ListAsync(
        Guid? patientId,
        string? kind,
        DocumentReferenceStatus? status,
        DocumentReferenceSource? source,
        int take,
        CancellationToken cancellationToken)
    {
        var query = _db.DocumentReferences
            .AsNoTracking()
            .Include(d => d.Signatures)
            .AsQueryable();

        // Default: exclude soft-deleted unless the caller explicitly asks for EnteredInError.
        if (status is { } s)
            query = query.Where(d => d.Status == s);
        else
            query = query.Where(d => d.Status != DocumentReferenceStatus.EnteredInError);

        if (patientId is { } pid)
            query = query.Where(d => d.PatientId == pid);
        if (!string.IsNullOrWhiteSpace(kind))
            query = query.Where(d => d.Kind == kind);
        if (source is { } src)
            query = query.Where(d => d.Source == src);

        return await query
            .OrderByDescending(d => d.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DocumentReference>> ListExpiredAsync(
        string kind,
        DateTime createdBefore,
        int take,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        return await _db.DocumentReferences
            .Where(d => d.Status == DocumentReferenceStatus.Current
                && d.Kind == kind
                && d.CreatedAtUtc < createdBefore)
            .OrderBy(d => d.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DocumentReference>> ListForPatientAsync(
        Guid patientId, CancellationToken cancellationToken) =>
        await _db.DocumentReferences
            .Where(d => d.Status == DocumentReferenceStatus.Current && d.PatientId == patientId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
