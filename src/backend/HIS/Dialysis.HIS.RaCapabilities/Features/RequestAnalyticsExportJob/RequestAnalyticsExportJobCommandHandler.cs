using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.RaCapabilities.Domain;
using Dialysis.HIS.RaCapabilities.Ports;

namespace Dialysis.HIS.RaCapabilities.Features.RequestAnalyticsExportJob;

public sealed class RequestAnalyticsExportJobCommandHandler(IRaCapabilityCommandStore store, IUnitOfWork unitOfWork)
    : ICommandHandler<RequestAnalyticsExportJobCommand, Guid>
{
    public async Task<Guid> Handle(RequestAnalyticsExportJobCommand request, CancellationToken cancellationToken)
    {
        var id = Guid.CreateVersion7();
        store.AddAnalyticsExportJob(
            new RaAnalyticsExportJob
            {
                Id = id,
                PipelineCode = request.PipelineCode.Trim(),
                RequestedAtUtc = DateTime.UtcNow,
                StatusCode = "Requested",
            });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
