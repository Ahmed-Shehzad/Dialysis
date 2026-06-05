using Dialysis.CQRS.Commands;
using Dialysis.HIE.Outbound.CareSummary;

namespace Dialysis.HIE.Outbound.Features.GenerateCareSummary;

public sealed class GenerateCareSummaryCommandHandler : ICommandHandler<GenerateCareSummaryCommand, CareSummaryResult>
{
    private readonly CareSummaryAssembler _assembler;
    public GenerateCareSummaryCommandHandler(CareSummaryAssembler assembler) => _assembler = assembler;

    public Task<CareSummaryResult> HandleAsync(GenerateCareSummaryCommand request, CancellationToken cancellationToken) =>
        _assembler.AssembleAndEnqueueAsync(request.PatientId, request.Purpose, cancellationToken);
}
