using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Exceptions;
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
    public IngestDeviceReadingCommandHandler(SlidingWindowRateLimiter rateLimiter,
        IDeviceReadingRepository repository,
        IDeviceRepository devices,
        IOptions<DeviceIngestionOptions> options)
    {
        _rateLimiter = rateLimiter;
        _repository = repository;
        _devices = devices;
        _options = options.Value;
    }
    public async Task<Guid> HandleAsync(IngestDeviceReadingCommand request, CancellationToken cancellationToken)
    {
        _rateLimiter.ThrowIfExceeded(request.DeviceId);

        // Govern the reading against the device registry: a registered device's status + patient
        // binding are enforced and its last-seen is stamped. Unknown devices are rejected only when
        // RequireRegistration is on, so the registry can roll out without breaking existing ingest.
        await GovernAgainstRegistryAsync(request, cancellationToken).ConfigureAwait(false);

        // Normalize the dedup key once so the fast-path read and the stored value match.
        var externalMessageId = string.IsNullOrWhiteSpace(request.ExternalMessageId)
            ? null
            : request.ExternalMessageId.Trim();

        if (externalMessageId is not null)
        {
            var existing = await _repository
                .FindIdByExternalMessageIdAsync(externalMessageId, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
                return existing.Value;
        }

        var record = new DeviceReadingRecord
        {
            Id = request.ReadingId != Guid.Empty ? request.ReadingId : Guid.CreateVersion7(),
            DeviceId = request.DeviceId,
            PatientId = request.PatientId,
            PayloadJson = request.PayloadJson,
            ReceivedAtUtc = DateTime.UtcNow,
            ExternalMessageId = externalMessageId,
        };
        // PersistIdempotentAsync flushes the reading + the device's last-seen stamp, and resolves a
        // concurrent ExternalMessageId collision to the winning row's id (idempotent under races).
        return await _repository.PersistIdempotentAsync(record, cancellationToken).ConfigureAwait(false);
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
