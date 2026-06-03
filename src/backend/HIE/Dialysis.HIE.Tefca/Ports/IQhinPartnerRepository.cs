using Dialysis.HIE.Tefca.Domain;

namespace Dialysis.HIE.Tefca.Ports;

/// <summary>
/// Repository for the <see cref="QhinPartner"/> aggregate. The admin controller drives
/// upserts / lifecycle transitions / trust-anchor management; the EF implementation lives
/// in <c>Dialysis.HIE.Persistence</c>.
/// </summary>
public interface IQhinPartnerRepository
{
    void Add(QhinPartner partner);

    void Remove(QhinPartner partner);

    Task<QhinPartner?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<QhinPartner>> ListAsync(CancellationToken cancellationToken);
}
