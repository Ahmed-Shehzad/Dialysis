using BuildingBlocks.Tenancy;
using BuildingBlocks.TimeSync;

using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Domain.ValueObjects;
using Dialysis.Prescription.Application.Exceptions;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using PrescriptionAggregate = Dialysis.Prescription.Application.Domain.Prescription;

namespace Dialysis.Prescription.Application.Features.IngestRspK22Message;

internal sealed class IngestRspK22MessageCommandHandler : ICommandHandler<IngestRspK22MessageCommand, IngestRspK22MessageResponse>
{
    private readonly IRspK22Parser _parser;
    private readonly IRspK22Validator _validator;
    private readonly IPrescriptionRepository _repository;
    private readonly ITenantContext _tenant;
    private readonly TimeSyncOptions _timeSync;
    private readonly ILogger<IngestRspK22MessageCommandHandler> _logger;

    public IngestRspK22MessageCommandHandler(IRspK22Parser parser, IRspK22Validator validator, IPrescriptionRepository repository, ITenantContext tenant, IOptions<TimeSyncOptions> timeSync, ILogger<IngestRspK22MessageCommandHandler> logger)
    {
        _parser = parser;
        _validator = validator;
        _repository = repository;
        _tenant = tenant;
        _timeSync = timeSync.Value;
        _logger = logger;
    }

    public async Task<IngestRspK22MessageResponse> HandleAsync(IngestRspK22MessageCommand request, CancellationToken cancellationToken = default)
    {
        DateTimeOffset? messageTimestamp = Hl7TimeSyncHelper.ExtractMessageTimestamp(request.RawHl7Message);
        CheckTimestampDrift(messageTimestamp);

        RspK22ParseResult result = _parser.Parse(request.RawHl7Message);

        RspK22ValidationResult validation = _validator.Validate(result, request.ValidationContext);
        if (!validation.IsValid)
            throw new RspK22ValidationException(validation.ErrorCode!, validation.Message ?? "RSP^K22 validation failed.", validation.Message);

        var orderId = new OrderId(result.OrderId);
        PrescriptionAggregate? existing = await _repository.GetByOrderIdAsync(orderId, cancellationToken);
        if (existing is not null)
            switch (request.ConflictPolicy)
            {
                case PrescriptionConflictPolicy.Reject:
                    throw new PrescriptionConflictException(orderId.Value);
                case PrescriptionConflictPolicy.Callback:
                    throw new PrescriptionConflictException(orderId.Value, existing.CallbackPhone);
                case PrescriptionConflictPolicy.Replace:
                    _repository.Delete(existing);
                    break;
                case PrescriptionConflictPolicy.Ignore:
                    return new IngestRspK22MessageResponse(result.OrderId, result.PatientMrn.Value, result.Settings.Count, true);
                case PrescriptionConflictPolicy.Partial:
                    var merged = PrescriptionAggregate.Create(orderId, result.PatientMrn, result.Modality ?? existing.Modality, result.OrderingProvider ?? existing.OrderingProvider, result.CallbackPhone ?? existing.CallbackPhone, _tenant.TenantId);
                    foreach (ProfileSetting s in existing.Settings)
                        merged.AddSetting(s);

                    var existingCodes = existing.Settings.Select(s => s.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    int addedCount = 0;

                    IEnumerable<ProfileSetting> settings = result.Settings.Where(s => existingCodes.Add(s.Code));
                    foreach (ProfileSetting setting in settings)
                    {
                        merged.AddSetting(setting);
                        addedCount++;
                    }

                    merged.CompleteIngestion();
                    _repository.Delete(existing);
                    await _repository.AddAsync(merged, cancellationToken);
                    await _repository.SaveChangesAsync(cancellationToken);
                    return new IngestRspK22MessageResponse(result.OrderId, result.PatientMrn.Value, addedCount, true);
            }

        var prescription = PrescriptionAggregate.Create(
            orderId,
            result.PatientMrn,
            result.Modality,
            result.OrderingProvider,
            result.CallbackPhone,
            _tenant.TenantId);

        foreach (ProfileSetting setting in result.Settings)
            prescription.AddSetting(setting);

        prescription.CompleteIngestion();
        await _repository.AddAsync(prescription, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new IngestRspK22MessageResponse(result.OrderId, result.PatientMrn.Value, result.Settings.Count, true);
    }

    private void CheckTimestampDrift(DateTimeOffset? messageTimestamp)
    {
        if (_timeSync.MaxAllowedDriftSeconds <= 0 || !_timeSync.LogDriftWarnings) return;

        double? drift = Hl7TimeSyncHelper.GetDriftSeconds(messageTimestamp);
        if (drift.HasValue && drift.Value > _timeSync.MaxAllowedDriftSeconds)
            _logger.LogWarning(
                "HL7 RSP^K22 message timestamp drift exceeds threshold. MessageTime={MessageTime}, DriftSeconds={Drift:F0}, MaxAllowed={MaxAllowed}",
                messageTimestamp, drift.Value, _timeSync.MaxAllowedDriftSeconds);
    }
}
