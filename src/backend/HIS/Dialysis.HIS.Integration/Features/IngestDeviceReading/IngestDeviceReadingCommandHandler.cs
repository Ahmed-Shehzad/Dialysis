using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.HIS.Integration.DeviceIngestion;
using Dialysis.HIS.Integration.DeviceRegistry;
using Microsoft.Extensions.Options;

namespace Dialysis.HIS.Integration.Features.IngestDeviceReading;

public sealed class IngestDeviceReadingCommandHandler : ICommandHandler<IngestDeviceReadingCommand, Guid>
{
    private readonly SlidingWindowRateLimiter _rateLimiter;
    private readonly IDeviceReadingRepository _repository;
    private readonly IDeviceRepository _devices;
    private readonly DeviceIngestionOptions _options;
    private readonly IUnitOfWork _unitOfWork;
    public IngestDeviceReadingCommandHandler(SlidingWindowRateLimiter rateLimiter,
        IDeviceReadingRepository repository,
        IDeviceRepository devices,
        IOptions<DeviceIngestionOptions> options,
        IUnitOfWork unitOfWork)
    {
        _rateLimiter = rateLimiter;
        _repository = repository;
        _devices = devices;
        _options = options.Value;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(IngestDeviceReadingCommand request, CancellationToken cancellationToken)
    {
        _rateLimiter.ThrowIfExceeded(request.DeviceId);

        // Govern the reading against the device registry: a registered device's status + patient
        // binding are enforced and its last-seen is stamped. Unknown devices are rejected only when
        // RequireRegistration is on, so the registry can roll out without breaking existing ingest.
        await GovernAgainstRegistryAsync(request, cancellationToken).ConfigureAwait(false);

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

    private async Task GovernAgainstRegistryAsync(IngestDeviceReadingCommand request, CancellationToken cancellationToken)
    {
        var device = await _devices
            .FindByDeviceIdAsync(request.DeviceId, cancellationToken)
            .ConfigureAwait(false);

        if (device is null)
        {
            if (_options.RequireRegistration)
                throw new DomainException($"Device '{request.DeviceId}' is not registered.");
            return;
        }

        if (!device.CanReport)
            throw new DomainException($"Device '{request.DeviceId}' is {device.Status} and may not report readings.");

        // A bound device's readings must match its patient — provenance guard against mis-routed data.
        if (device.PatientId is { } boundPatient && boundPatient != request.PatientId)
            throw new DomainException($"Device '{request.DeviceId}' is bound to a different patient.");

        device.RecordSeen(DateTime.UtcNow);
    }
}
