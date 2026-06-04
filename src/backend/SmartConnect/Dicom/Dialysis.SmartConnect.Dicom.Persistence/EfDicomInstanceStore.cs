using Dialysis.SmartConnect.Persistence.EntityFrameworkCore;
using Dialysis.SmartConnect.Persistence.EntityFrameworkCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.SmartConnect.Dicom.Persistence;

/// <summary>
/// EF-backed implementation of <see cref="IDicomInstanceStore"/>. Reads + writes
/// <see cref="DicomInstanceEntity"/> rows in the SmartConnect DbContext.
/// </summary>
public sealed class EfDicomInstanceStore : IDicomInstanceStore
{
    private readonly SmartConnectDbContext _db;
    /// <summary>
    /// EF-backed implementation of <see cref="IDicomInstanceStore"/>. Reads + writes
    /// <see cref="DicomInstanceEntity"/> rows in the SmartConnect DbContext.
    /// </summary>
    public EfDicomInstanceStore(SmartConnectDbContext db) => _db = db;
    public async Task AddAsync(DicomInstanceMetadata metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        _db.DicomInstances.Add(new DicomInstanceEntity
        {
            Id = Guid.CreateVersion7(),
            StudyInstanceUid = metadata.StudyInstanceUid,
            SeriesInstanceUid = metadata.SeriesInstanceUid,
            SopInstanceUid = metadata.SopInstanceUid,
            SopClassUid = metadata.SopClassUid,
            PatientId = metadata.PatientId,
            PatientName = metadata.PatientName,
            Modality = metadata.Modality,
            ReceivedUtc = metadata.ReceivedUtc,
            SizeBytes = metadata.SizeBytes,
            BlobId = metadata.BlobId,
        });
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<DicomInstanceMetadata?> GetAsync(string sopInstanceUid, CancellationToken cancellationToken)
    {
        var row = await _db.DicomInstances.AsNoTracking()
            .FirstOrDefaultAsync(i => i.SopInstanceUid == sopInstanceUid, cancellationToken).ConfigureAwait(false);
        return row is null ? null : Project(row);
    }

    public async Task<IReadOnlyList<DicomInstanceMetadata>> GetByStudyAsync(
        string studyInstanceUid, CancellationToken cancellationToken)
    {
        var rows = await _db.DicomInstances.AsNoTracking()
            .Where(i => i.StudyInstanceUid == studyInstanceUid)
            .OrderBy(i => i.SeriesInstanceUid).ThenBy(i => i.SopInstanceUid)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return rows.ConvertAll(Project);
    }

    public async Task<IReadOnlyList<DicomStudy>> SearchStudiesAsync(
        string? patientId,
        DateTimeOffset? studyDateFrom,
        DateTimeOffset? studyDateTo,
        CancellationToken cancellationToken)
    {
        var query = _db.DicomInstances.AsNoTracking().AsQueryable();
        if (!string.IsNullOrEmpty(patientId))
        {
            query = query.Where(i => i.PatientId == patientId);
        }
        if (studyDateFrom is not null)
        {
            query = query.Where(i => i.ReceivedUtc >= studyDateFrom);
        }
        if (studyDateTo is not null)
        {
            query = query.Where(i => i.ReceivedUtc <= studyDateTo);
        }
        var rows = await query.ToListAsync(cancellationToken).ConfigureAwait(false);

        return [.. rows
            .GroupBy(i => i.StudyInstanceUid)
            .Select(g =>
            {
                var first = g.First();
                var series = g.GroupBy(i => i.SeriesInstanceUid)
                    .Select(sg => new DicomSeries(
                        sg.Key,
                        sg.First().Modality,
                        [.. sg.Select(Project)]))
                    .ToList();
                return new DicomStudy(
                    g.Key,
                    first.PatientId,
                    first.PatientName,
                    g.Min(i => i.ReceivedUtc),
                    series);
            })];
    }

    private static DicomInstanceMetadata Project(DicomInstanceEntity row) => new(
        row.StudyInstanceUid,
        row.SeriesInstanceUid,
        row.SopInstanceUid,
        row.SopClassUid,
        row.PatientId,
        row.PatientName,
        row.Modality,
        row.ReceivedUtc,
        row.SizeBytes,
        row.BlobId);
}
