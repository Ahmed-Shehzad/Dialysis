using Dialysis.CQRS.Commands;
using Dialysis.DomainDrivenDesign.Persistence;
using Dialysis.EHR.Registration.Domain;
using Dialysis.EHR.Registration.Ports;

namespace Dialysis.EHR.Registration.Features.RegisterProvider;

public sealed class RegisterProviderCommandHandler(
    IProviderRepository providers,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterProviderCommand, Guid>
{
    public async Task<Guid> HandleAsync(RegisterProviderCommand request, CancellationToken cancellationToken)
    {
        if (await providers.FindByNpiAsync(request.NationalProviderIdentifier, cancellationToken).ConfigureAwait(false) is not null)
            throw new InvalidOperationException($"NPI '{request.NationalProviderIdentifier}' is already registered.");

        var id = Guid.CreateVersion7();
        var name = new HumanName(request.FamilyName, request.GivenName);
        var provider = Provider.Register(
            id,
            request.NationalProviderIdentifier,
            name,
            request.Kind,
            request.SpecialtyCode,
            request.LicenseNumber);

        providers.Add(provider);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }
}
