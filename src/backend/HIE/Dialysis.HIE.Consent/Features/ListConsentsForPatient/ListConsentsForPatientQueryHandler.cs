using Dialysis.CQRS.Queries;
using Dialysis.HIE.Consent.Ports;

namespace Dialysis.HIE.Consent.Features.ListConsentsForPatient;

public sealed class ListConsentsForPatientQueryHandler(IConsentRepository repository)
    : IQueryHandler<ListConsentsForPatientQuery, IReadOnlyList<ConsentDto>>
{
    public async Task<IReadOnlyList<ConsentDto>> HandleAsync(ListConsentsForPatientQuery request, CancellationToken cancellationToken)
    {
        var rows = await repository.ListForPatientAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        return [.. rows
            .Select(c => new ConsentDto(
                c.Id, c.PatientId, c.PartnerId, c.Scope, c.Direction,
                c.EffectiveFromUtc, c.EffectiveToUtc, c.RevokedAtUtc))];
    }
}
