using Dialysis.CQRS.Queries;
using Dialysis.HIE.Consent.Ports;

namespace Dialysis.HIE.Consent.Features.ListConsentsForPatient;

public sealed class ListConsentsForPatientQueryHandler : IQueryHandler<ListConsentsForPatientQuery, IReadOnlyList<ConsentDto>>
{
    private readonly IConsentRepository _repository;
    public ListConsentsForPatientQueryHandler(IConsentRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<ConsentDto>> HandleAsync(ListConsentsForPatientQuery request, CancellationToken cancellationToken)
    {
        var rows = await _repository.ListForPatientAsync(request.PatientId, cancellationToken).ConfigureAwait(false);
        return [.. rows
            .Select(c => new ConsentDto(
                c.Id, c.PatientId, c.PartnerId, c.Scope, c.Direction,
                c.EffectiveFromUtc, c.EffectiveToUtc, c.RevokedAtUtc))];
    }
}
