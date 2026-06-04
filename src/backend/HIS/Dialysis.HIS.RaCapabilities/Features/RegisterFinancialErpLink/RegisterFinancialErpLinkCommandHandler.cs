using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterFinancialErpLink;

public sealed class RegisterFinancialErpLinkCommandHandler : ICommandHandler<RegisterFinancialErpLinkCommand, Guid>
{
    private readonly IRaCapabilityCommandStore _store;
    private readonly IUnitOfWork _unitOfWork;
    public RegisterFinancialErpLinkCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RegisterFinancialErpLinkCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        _store.AddFinancialErpLink(
            new RaFinancialErpLink
            {
                Id = id,
                SystemCode = request.SystemCode.Trim(),
                StatusCode = request.StatusCode.Trim(),
                LastHandshakeAtUtc = DateTime.UtcNow,
            });
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
