using BuildingBlocks.ValueObjects;

using Dialysis.Prescription.Application.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Prescription.Application.Features.ProcessQbpD01Query;

internal sealed class ProcessQbpD01QueryCommandHandler : ICommandHandler<ProcessQbpD01QueryCommand, ProcessQbpD01QueryResponse>
{
    private readonly IQbpD01Parser _qbpParser;
    private readonly IRspK22Builder _rspBuilder;
    private readonly IPrescriptionRepository _repository;

    public ProcessQbpD01QueryCommandHandler(IQbpD01Parser qbpParser, IRspK22Builder rspBuilder, IPrescriptionRepository repository)
    {
        _qbpParser = qbpParser;
        _rspBuilder = rspBuilder;
        _repository = repository;
    }

    public async Task<ProcessQbpD01QueryResponse> HandleAsync(ProcessQbpD01QueryCommand request, CancellationToken cancellationToken = default)
    {
        QbpD01ParseResult query = _qbpParser.Parse(request.RawHl7Message);

        var context = new RspK22ValidationContext(
            query.MessageControlId,
            query.QueryTag,
            query.QueryName);

        Domain.Prescription? prescription = await _repository.GetLatestByMrnAsync(
            new MedicalRecordNumber(query.Mrn),
            cancellationToken);

        string rspK22 = prescription is null
            ? _rspBuilder.BuildNoDataFound(context, query.Mrn)
            : _rspBuilder.BuildFromPrescription(prescription, context);

        return new ProcessQbpD01QueryResponse(rspK22);
    }
}
