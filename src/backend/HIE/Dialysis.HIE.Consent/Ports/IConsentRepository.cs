using Dialysis.HIE.Consent.Domain;

namespace Dialysis.HIE.Consent.Ports;

public interface IConsentRepository
{
    Task<ConsentRecord?> FindActiveAsync(Guid patientId, string partnerId, string scope, ConsentDirection direction, DateTime atUtc, string? purpose = null, CancellationToken cancellationToken = default);

    Task<ConsentRecord?> FindActiveByExternalReferenceAsync(string externalPatientReference, string partnerId, string scope, ConsentDirection direction, DateTime atUtc, string? purpose = null, CancellationToken cancellationToken = default);

    Task<ConsentRecord?> GetAsync(Guid consentId, CancellationToken cancellationToken = default);

    Task AddAsync(ConsentRecord consent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConsentRecord>> ListForPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
}
