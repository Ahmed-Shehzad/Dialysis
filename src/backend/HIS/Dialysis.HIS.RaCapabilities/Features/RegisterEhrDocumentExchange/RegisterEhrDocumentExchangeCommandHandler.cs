using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterEhrDocumentExchange;

public sealed class RegisterEhrDocumentExchangeCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterEhrDocumentExchangeCommand, Guid>
{
    public async Task<Guid> Handle(RegisterEhrDocumentExchangeCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var at = request.ExchangedAtUtc ?? DateTime.UtcNow;
        store.AddEhrDocumentExchangeRecord(
            new RaEhrDocumentExchangeRecord
            {
                Id = id,
                PatientId = request.PatientId,
                DocumentTypeCode = request.DocumentTypeCode.Trim(),
                ExternalSystemCode = request.ExternalSystemCode.Trim(),
                ExternalUri = request.ExternalUri.Trim(),
                ExchangedAtUtc = at,
            });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
