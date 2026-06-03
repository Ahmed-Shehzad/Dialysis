using Dialysis.HIE.Tefca.Domain;
using Dialysis.HIE.Tefca.Ports;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.Repositories;

public sealed class EfQhinPartnerRepository(HieDbContext db) : IQhinPartnerRepository
{
    public void Add(QhinPartner partner)
    {
        ArgumentNullException.ThrowIfNull(partner);
        db.QhinPartners.Add(partner);
    }

    public void Remove(QhinPartner partner)
    {
        ArgumentNullException.ThrowIfNull(partner);
        db.QhinPartners.Remove(partner);
    }

    public Task<QhinPartner?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        db.QhinPartners
            .Include(p => p.TrustAnchors)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<QhinPartner>> ListAsync(CancellationToken cancellationToken) =>
        await db.QhinPartners
            .AsNoTracking()
            .Include(p => p.TrustAnchors)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
