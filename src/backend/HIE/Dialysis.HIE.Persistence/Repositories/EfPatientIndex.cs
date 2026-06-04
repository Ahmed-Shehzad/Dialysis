using Dialysis.HIE.Inbound.Domain;
using Dialysis.HIE.Inbound.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfPatientIndex : IPatientIndex
{
    private readonly HieDbContext _db;
    public EfPatientIndex(HieDbContext db) => _db = db;
    public async Task UpsertAsync(PatientIndexEntry entry, CancellationToken cancellationToken = default)
    {
        var existing = await _db.PatientIndexEntries
            .FirstOrDefaultAsync(p => p.PartnerId == entry.PartnerId && p.ExternalLogicalId == entry.ExternalLogicalId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            await _db.PatientIndexEntries.AddAsync(entry, cancellationToken).ConfigureAwait(false);
            return;
        }
        existing.Refresh(entry.MedicalRecordNumber, entry.FamilyName, entry.GivenName, entry.DateOfBirth, entry.SexAtBirthCode, entry.UpdatedAtUtc);
    }

    public async Task<IReadOnlyList<PatientIndexEntry>> MatchAsync(
        string? medicalRecordNumber,
        string? familyName,
        string? givenName,
        DateOnly? dateOfBirth,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.PatientIndexEntries.AsQueryable();
        if (!string.IsNullOrWhiteSpace(medicalRecordNumber))
            query = query.Where(p => p.MedicalRecordNumber == medicalRecordNumber);
        if (!string.IsNullOrWhiteSpace(familyName))
            query = query.Where(p => p.FamilyName == familyName);
        if (!string.IsNullOrWhiteSpace(givenName))
            query = query.Where(p => p.GivenName == givenName);
        if (dateOfBirth is { } dob)
            query = query.Where(p => p.DateOfBirth == dob);
        return await query.Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
