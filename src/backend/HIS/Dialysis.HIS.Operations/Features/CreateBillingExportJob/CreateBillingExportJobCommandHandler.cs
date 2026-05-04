using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Operations.Domain;
using Dialysis.HIS.Operations.Ports;

namespace Dialysis.HIS.Operations.Features.CreateBillingExportJob;

public sealed class CreateBillingExportJobCommandHandler(IBillingExportRepository billing, IUnitOfWork unitOfWork)
    : ICommandHandler<CreateBillingExportJobCommand, Guid>
{
    public async Task<Guid> Handle(CreateBillingExportJobCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        billing.Add(new BillingExportJob
        {
            Id = id,
            RequestedAtUtc = DateTime.UtcNow,
            FormatCode = string.IsNullOrWhiteSpace(request.FormatCode) ? "FHIR_BUNDLE_STUB" : request.FormatCode.Trim(),
            StatusCode = "Queued",
            PayerCode = string.IsNullOrWhiteSpace(request.PayerCode) ? null : request.PayerCode.Trim(),
        });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
