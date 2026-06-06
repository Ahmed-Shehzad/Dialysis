using Dialysis.BuildingBlocks.Transponder;
using Dialysis.EHR.Contracts.Integration;

namespace Dialysis.Simulation.Drivers.Http;

/// <summary>Drives the real EHR API and publishes the encounter-closed event for charge capture.</summary>
public sealed class HttpEhrDriver : IEhrDriver
{
    private readonly HttpClient _client;
    private readonly ITransponderBus _bus;

    /// <summary>Creates the driver.</summary>
    public HttpEhrDriver(HttpClient client, ITransponderBus bus)
    {
        _client = client;
        _bus = bus;
    }

    /// <inheritdoc />
    public async Task<RegisteredPatient> RegisterPatientAsync(RegisterPatientCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = await HttpDriverJson.PostReadIdAsync(_client, "api/v1.0/clinical/patients",
            new
            {
                command.MedicalRecordNumber,
                command.FamilyName,
                command.GivenName,
                command.DateOfBirth,
                command.SexAtBirthCode,
            },
            context, cancellationToken).ConfigureAwait(false);
        return new RegisteredPatient(id);
    }

    /// <inheritdoc />
    public async Task<StartedEncounter> StartEncounterAsync(StartEncounterCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = await HttpDriverJson.PostReadIdAsync(_client, "api/v1.0/clinical/encounters",
            new
            {
                command.PatientId,
                command.ProviderId,
                command.EncounterClassCode,
                command.AppointmentId,
            },
            context, cancellationToken).ConfigureAwait(false);
        return new StartedEncounter(id);
    }

    /// <inheritdoc />
    public async Task<ClosedEncounter> CloseEncounterAsync(CloseEncounterCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        // No close-encounter HTTP write surface: publish the integration event the EHR billing consumer
        // turns into captured charges (gated by the EHR charge-automation flag). Charges land
        // asynchronously, so the captured set is verified out-of-band, not returned here.
        var @event = new EncounterClosedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: DateTime.UtcNow,
            SchemaVersion: 1,
            EncounterId: command.EncounterId,
            PatientId: command.PatientId,
            ProviderId: command.ProviderId,
            ClosedAtUtc: DateTime.UtcNow,
            DiagnosisIcd10Codes: command.DiagnosisIcd10Codes,
            ProcedureCptCodes: command.ProcedureCptCodes);

        await _bus.PublishAsync(@event, new TransponderPublishOptions(context.CorrelationId), cancellationToken).ConfigureAwait(false);
        return new ClosedEncounter(command.EncounterId, []);
    }

    /// <inheritdoc />
    public async Task<RequestedReferral> RequestReferralAsync(RequestReferralCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = await HttpDriverJson.PostReadIdAsync(_client, "api/v1.0/clinical/referrals",
            new
            {
                command.PatientId,
                command.DestinationPartnerId,
                command.ReferringProviderId,
                command.ReferralReason,
            },
            context, cancellationToken).ConfigureAwait(false);
        return new RequestedReferral(id);
    }
}
