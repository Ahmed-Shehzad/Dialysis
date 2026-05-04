using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Integration.DeviceIngestion;

namespace Dialysis.HIS.Integration.Features.IngestDeviceReading;

public sealed class IngestDeviceReadingCommandHandler(
    SlidingWindowRateLimiter rateLimiter,
    IDeviceReadingRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<IngestDeviceReadingCommand, Guid>
{
    public async Task<Guid> Handle(IngestDeviceReadingCommand request, CancellationToken cancellationToken)
    {
        rateLimiter.ThrowIfExceeded(request.DeviceId);
        if (!string.IsNullOrWhiteSpace(request.ExternalMessageId))
        {
            var existing = await repository
                .FindIdByExternalMessageIdAsync(request.ExternalMessageId, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
                return existing.Value;
        }

        var id = Guid.CreateVersion7();
        repository.Add(new DeviceReadingRecord
        {
            Id = id,
            DeviceId = request.DeviceId,
            PatientId = request.PatientId,
            PayloadJson = request.PayloadJson,
            ReceivedAtUtc = DateTime.UtcNow,
            ExternalMessageId = string.IsNullOrWhiteSpace(request.ExternalMessageId) ? null : request.ExternalMessageId.Trim(),
        });
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
