using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.PatientChart.Domain;
using Dialysis.EHR.PatientChart.Ports;

namespace Dialysis.EHR.PatientChart.Features.RecordAllergy;

public sealed class RecordAllergyCommandHandler : ICommandHandler<RecordAllergyCommand, Guid>
{
    private readonly IAllergyRepository _allergies;
    private readonly IUnitOfWork _unitOfWork;
    public RecordAllergyCommandHandler(IAllergyRepository allergies,
        IUnitOfWork unitOfWork)
    {
        _allergies = allergies;
        _unitOfWork = unitOfWork;
    }
    public async Task<Guid> HandleAsync(RecordAllergyCommand request, CancellationToken cancellationToken)
    {
        var allergen = new Coding(request.AllergenSystem, request.AllergenCode, request.AllergenDisplay);
        var id = request.AllergyId != Guid.Empty ? request.AllergyId : Guid.CreateVersion7();
        var allergy = Allergy.Record(
            id,
            request.PatientId,
            allergen,
            request.Severity,
            request.VerificationStatus,
            request.ReactionText,
            request.OnsetDate);
        _allergies.Add(allergy);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
