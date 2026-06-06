namespace Dialysis.Simulation.Drivers.Http;

/// <summary>Drives the real HIS scheduling + patient-flow API.</summary>
public sealed class HttpHisDriver : IHisDriver
{
    private readonly HttpClient _client;

    /// <summary>Creates the driver.</summary>
    public HttpHisDriver(HttpClient client) => _client = client;

    /// <inheritdoc />
    public async Task<BookedAppointment> BookAppointmentAsync(BookAppointmentCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = await HttpDriverJson.PostReadIdAsync(_client, "api/v1.0/scheduling/appointments",
            new
            {
                command.PatientId,
                command.ProviderId,
                command.SlotStartUtc,
                command.SlotEndUtc,
            },
            context, cancellationToken).ConfigureAwait(false);
        return new BookedAppointment(id);
    }

    /// <inheritdoc />
    public async Task<AdmittedPatient> AdmitPatientAsync(AdmitPatientCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = await HttpDriverJson.PostReadIdAsync(_client, "api/v1.0/patient-flow/admissions",
            new
            {
                command.PatientId,
                command.WardCode,
            },
            context, cancellationToken).ConfigureAwait(false);
        return new AdmittedPatient(id);
    }

    /// <inheritdoc />
    public async Task<DischargedPatient> DischargePatientAsync(DischargePatientCommand command, DriverContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await HttpDriverJson.PostNoContentAsync(
            _client, $"api/v1.0/patient-flow/admissions/{command.AdmissionId}/discharge", context, cancellationToken)
            .ConfigureAwait(false);
        return new DischargedPatient(command.AdmissionId);
    }
}
