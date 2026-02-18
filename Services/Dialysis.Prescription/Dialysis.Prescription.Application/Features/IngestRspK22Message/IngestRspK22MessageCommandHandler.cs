using BuildingBlocks.Tenancy;

using Dialysis.Prescription.Application.Abstractions;
using Dialysis.Prescription.Application.Domain;
using Dialysis.Prescription.Application.Exceptions;

using Intercessor.Abstractions;

using PrescriptionAggregate = Dialysis.Prescription.Application.Domain.Prescription;

namespace Dialysis.Prescription.Application.Features.IngestRspK22Message;

internal sealed class IngestRspK22MessageCommandHandler : ICommandHandler<IngestRspK22MessageCommand, IngestRspK22MessageResponse>
{
    private readonly IRspK22Parser _parser;
    private readonly IRspK22Validator _validator;
    private readonly IPrescriptionRepository _repository;
    private readonly ITenantContext _tenant;

    public IngestRspK22MessageCommandHandler(IRspK22Parser parser, IRspK22Validator validator, IPrescriptionRepository repository, ITenantContext tenant)
    {
        _parser = parser;
        _validator = validator;
        _repository = repository;
        _tenant = tenant;
    }

    public async Task<IngestRspK22MessageResponse> HandleAsync(IngestRspK22MessageCommand request, CancellationToken cancellationToken = default)
    {
        RspK22ParseResult result = _parser.Parse(request.RawHl7Message);

        RspK22ValidationResult validation = _validator.Validate(result, request.ValidationContext);
        if (!validation.IsValid)
            throw new RspK22ValidationException(validation.ErrorCode!, validation.Message ?? "RSP^K22 validation failed.", validation.Message);

        PrescriptionAggregate? existing = await _repository.GetByOrderIdAsync(result.OrderId, cancellationToken);
        if (existing is not null)
            switch (request.ConflictPolicy)
            {
                case PrescriptionConflictPolicy.Reject:
                    throw new PrescriptionConflictException(result.OrderId);
                case PrescriptionConflictPolicy.Replace:
                    _repository.Delete(existing);
                    break;
                case PrescriptionConflictPolicy.Ignore:
                    return new IngestRspK22MessageResponse(result.OrderId, result.PatientMrn.Value, result.Settings.Count, true);
            }

        var prescription = PrescriptionAggregate.Create(
            result.OrderId,
            result.PatientMrn,
            result.Modality,
            result.OrderingProvider,
            result.CallbackPhone,
            _tenant.TenantId);

        foreach (ProfileSetting setting in result.Settings)
            prescription.AddSetting(setting);

        await _repository.AddAsync(prescription, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new IngestRspK22MessageResponse(result.OrderId, result.PatientMrn.Value, result.Settings.Count, true);
    }
}
