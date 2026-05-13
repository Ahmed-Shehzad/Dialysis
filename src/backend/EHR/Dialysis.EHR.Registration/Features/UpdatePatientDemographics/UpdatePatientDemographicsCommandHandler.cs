using Dialysis.CQRS;
using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;

namespace Dialysis.EHR.Registration.Features.UpdatePatientDemographics;

public sealed class UpdatePatientDemographicsCommandHandler(
    IPatientRepository patients,
    IUnitOfWork unitOfWork)
    : ICommandHandler<UpdatePatientDemographicsCommand, Unit>
{
    public async Task<Unit> Handle(UpdatePatientDemographicsCommand request, CancellationToken cancellationToken)
    {
        var patient = await patients.GetAsync(request.PatientId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Patient '{request.PatientId}' not found.");

        var name = new HumanName(request.FamilyName, request.GivenName, request.MiddleName);
        var address = TryBuildAddress(request);
        patient.UpdateDemographics(name, request.SexAtBirthCode, request.PreferredLanguageCode, address);

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

    private static PostalAddress? TryBuildAddress(UpdatePatientDemographicsCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.AddressLine1)
            || string.IsNullOrWhiteSpace(request.City)
            || string.IsNullOrWhiteSpace(request.StateOrProvince)
            || string.IsNullOrWhiteSpace(request.PostalCode)
            || string.IsNullOrWhiteSpace(request.CountryCode))
            return null;
        return new PostalAddress(request.AddressLine1!, request.City!, request.StateOrProvince!, request.PostalCode!, request.CountryCode!, request.AddressLine2);
    }
}
