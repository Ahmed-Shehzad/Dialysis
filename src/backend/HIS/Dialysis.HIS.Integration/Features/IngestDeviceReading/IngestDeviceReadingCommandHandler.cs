using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Integration.DeviceIngestion;

namespace Dialysis.HIS.Integration.Features.IngestDeviceReading;

public sealed class IngestDeviceReadingCommandHandler : ICommandHandler<IngestDeviceReadingCommand, Guid>
{
    private readonly SlidingWindowRateLimiter _rateLimiter;
    private readonly IDeviceReadingRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public IngestDeviceReadingCommandHandler(SlidingWindowRateLimiter rateLimiter,
        IDeviceReadingRepository repository,
        IUnitOfWork unitOfWork)
    {
        _rateLimiter = rateLimiter;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(IngestDeviceReadingCommand request, CancellationToken cancellationToken)
    {
        _rateLimiter.ThrowIfExceeded(request.DeviceId);
        if (!string.IsNullOrWhiteSpace(request.ExternalMessageId))
        {
            var existing = await _repository
                .FindIdByExternalMessageIdAsync(request.ExternalMessageId, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
                return existing.Value;
        }

        var id = request.ReadingId != Guid.Empty ? request.ReadingId : Guid.CreateVersion7();
        _repository.Add(new DeviceReadingRecord
        {
            Id = id,
            DeviceId = request.DeviceId,
            PatientId = request.PatientId,
            PayloadJson = request.PayloadJson,
            ReceivedAtUtc = DateTime.UtcNow,
            ExternalMessageId = string.IsNullOrWhiteSpace(request.ExternalMessageId) ? null : request.ExternalMessageId.Trim(),
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
