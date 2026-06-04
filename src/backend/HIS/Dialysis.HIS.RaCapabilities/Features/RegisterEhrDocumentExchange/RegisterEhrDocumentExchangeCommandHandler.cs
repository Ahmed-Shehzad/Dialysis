using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RegisterEhrDocumentExchange;

public sealed class RegisterEhrDocumentExchangeCommandHandler : ICommandHandler<RegisterEhrDocumentExchangeCommand, Guid>
{
    private readonly IRaCapabilityCommandStore _store;
    private readonly IUnitOfWork _unitOfWork;
    public RegisterEhrDocumentExchangeCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    {
        _store = store;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RegisterEhrDocumentExchangeCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        var at = request.ExchangedAtUtc ?? DateTime.UtcNow;
        _store.AddEhrDocumentExchangeRecord(
            new RaEhrDocumentExchangeRecord
            {
                Id = id,
                PatientId = request.PatientId,
                DocumentTypeCode = request.DocumentTypeCode.Trim(),
                ExternalSystemCode = request.ExternalSystemCode.Trim(),
                ExternalUri = request.ExternalUri.Trim(),
                ExchangedAtUtc = at,
            });
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
