using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;

namespace Dialysis.EHR.PatientChart.Features.RecordAllergy;

public sealed class RecordAllergyCommandHandler(
    IAllergyRepository allergies,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RecordAllergyCommand, Guid>
{
    public async Task<Guid> Handle(RecordAllergyCommand request, CancellationToken cancellationToken)
    {
        var allergen = new Coding(request.AllergenSystem, request.AllergenCode, request.AllergenDisplay);
        var id = Guid.CreateVersion7();
        var allergy = Allergy.Record(
            id,
            request.PatientId,
            allergen,
            request.Severity,
            request.VerificationStatus,
            request.ReactionText,
            request.OnsetDate);
        allergies.Add(allergy);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
