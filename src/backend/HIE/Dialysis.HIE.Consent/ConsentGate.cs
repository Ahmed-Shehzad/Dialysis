using Dialysis.HIE.Consent.Domain;
using Dialysis.HIE.Consent.Ports;
using Dialysis.HIE.Core.Abstraction.Consent;

namespace Dialysis.HIE.Consent;

/// <summary>
/// Default <see cref="IConsentGate"/> implementation: looks up an active <see cref="ConsentRecord"/>
/// for the requested patient + partner + scope + direction. Fails closed when no record is found.
/// </summary>
public sealed class ConsentGate : IConsentGate
{
    private readonly IConsentRepository _repository;
    private readonly TimeProvider _timeProvider;
    /// <summary>
    /// Default <see cref="IConsentGate"/> implementation: looks up an active <see cref="ConsentRecord"/>
    /// for the requested patient + partner + scope + direction. Fails closed when no record is found.
    /// </summary>
    public ConsentGate(IConsentRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }
    public async Task<bool> CheckOutboundAsync(Guid patientId, string partnerId, string scope, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var consent = await _repository
            .FindActiveAsync(patientId, partnerId, scope, ConsentDirection.Outbound, now, cancellationToken)
            .ConfigureAwait(false);
        if (consent is not null) return true;

        var bidirectional = await _repository
            .FindActiveAsync(patientId, partnerId, scope, ConsentDirection.Bidirectional, now, cancellationToken)
            .ConfigureAwait(false);
        return bidirectional is not null;
    }

    public async Task<bool> CheckInboundAsync(string externalPatientReference, string partnerId, string scope, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var consent = await _repository
            .FindActiveByExternalReferenceAsync(externalPatientReference, partnerId, scope, ConsentDirection.Inbound, now, cancellationToken)
            .ConfigureAwait(false);
        if (consent is not null) return true;

        var bidirectional = await _repository
            .FindActiveByExternalReferenceAsync(externalPatientReference, partnerId, scope, ConsentDirection.Bidirectional, now, cancellationToken)
            .ConfigureAwait(false);
        return bidirectional is not null;
    }
}
