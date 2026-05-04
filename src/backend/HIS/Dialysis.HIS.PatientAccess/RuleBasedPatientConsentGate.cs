using Dialysis.HIS.PatientAccess.Ports;
using Microsoft.Extensions.Options;

namespace Dialysis.HIS.PatientAccess;

/// <summary>
/// Enforces persisted portal consent. Missing row = implicit allow-all (legacy patients); explicit rows can deny.
/// </summary>
public sealed class RuleBasedPatientConsentGate(
    IPatientPortalConsentReadModel consent,
    IOptions<PatientPortalOptions> portalOptions) : IPatientConsentGate
{
    private readonly PatientPortalOptions _portalOptions = portalOptions.Value;

    public async Task EnsureCanViewSummaryAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var state = await consent.GetAsync(patientId, cancellationToken).ConfigureAwait(false);
        if (_portalOptions.RequireExplicitConsentRowForPortal && state is null)
            throw new UnauthorizedAccessException("Patient portal requires an explicit consent preference record.");
        if (state is { SummaryVisible: false })
            throw new UnauthorizedAccessException("Patient portal summary is not visible for this patient.");
    }

    public async Task EnsureCanRequestAppointmentAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        var state = await consent.GetAsync(patientId, cancellationToken).ConfigureAwait(false);
        if (_portalOptions.RequireExplicitConsentRowForPortal && state is null)
            throw new UnauthorizedAccessException("Patient portal requires an explicit consent preference record.");
        if (state is { AppointmentRequestsAllowed: false })
            throw new UnauthorizedAccessException("Patient-initiated appointment requests are disabled for this patient.");
    }
}
