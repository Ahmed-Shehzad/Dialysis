using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterFinancialErpLink;

public sealed class RegisterFinancialErpLinkCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterFinancialErpLinkCommand, Guid>
{
    public async Task<Guid> Handle(RegisterFinancialErpLinkCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        store.AddFinancialErpLink(
            new RaFinancialErpLink
            {
                Id = id,
                SystemCode = request.SystemCode.Trim(),
                StatusCode = request.StatusCode.Trim(),
                LastHandshakeAtUtc = DateTime.UtcNow,
            });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
