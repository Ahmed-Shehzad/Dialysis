using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Contracts.CodeSets;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;

namespace Dialysis.EHR.PatientChart.Features.RecordImmunization;

public sealed class RecordImmunizationCommandHandler(
    IImmunizationRepository immunizations,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RecordImmunizationCommand, Guid>
{
    public async Task<Guid> HandleAsync(RecordImmunizationCommand request, CancellationToken cancellationToken)
    {
        var vaccine = new Coding(EhrCodeSystems.Cvx, request.CvxCode, request.CvxDisplay);
        var id = Guid.CreateVersion7();
        var immunization = Immunization.Record(
            id,
            request.PatientId,
            vaccine,
            request.AdministeredOn,
            request.LotNumber,
            request.Manufacturer,
            request.SiteCode,
            request.AdministeringProviderId);
        immunizations.Add(immunization);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
