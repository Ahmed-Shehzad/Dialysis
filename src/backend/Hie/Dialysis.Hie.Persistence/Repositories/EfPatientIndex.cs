using Dialysis.Hie.Inbound.Domain;
using Dialysis.Hie.Inbound.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.Hie.Persistence.Repositories;

public sealed class EfPatientIndex(HieDbContext db) : IPatientIndex
{
    public async Task UpsertAsync(PatientIndexEntry entry, CancellationToken cancellationToken = default)
    {
        var existing = await db.PatientIndexEntries
            .FirstOrDefaultAsync(p => p.PartnerId == entry.PartnerId && p.ExternalLogicalId == entry.ExternalLogicalId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            await db.PatientIndexEntries.AddAsync(entry, cancellationToken).ConfigureAwait(false);
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
        var query = db.PatientIndexEntries.AsQueryable();
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
