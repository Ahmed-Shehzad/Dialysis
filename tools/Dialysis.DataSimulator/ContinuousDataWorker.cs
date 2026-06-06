using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dialysis.DataSimulator;

/// <summary>
/// Continuously generates consistent, related patient journeys and drives them through the real module
/// write endpoints (register → schedule/admit → encounter → lab order → document), so the modules raise
/// their domain + integration events. Each call threads the ids produced by earlier calls, so the
/// generated records relate to one another. Per-call failures are logged and skipped so the loop keeps running.
/// </summary>
public sealed class ContinuousDataWorker : BackgroundService
{
    private readonly IEhrClient _ehr;
    private readonly IHisClient _his;
    private readonly ILabClient _lab;
    private readonly IHieClient _hie;
    private readonly PatientGenerator _generator;
    private readonly DataSimulatorOptions _options;
    private readonly ILogger<ContinuousDataWorker> _logger;
    private long _sequence;

    /// <summary>Creates the worker.</summary>
    public ContinuousDataWorker(
        IEhrClient ehr,
        IHisClient his,
        ILabClient lab,
        IHieClient hie,
        PatientGenerator generator,
        IOptions<DataSimulatorOptions> options,
        ILogger<ContinuousDataWorker> logger)
    {
        _ehr = ehr;
        _his = his;
        _lab = lab;
        _hie = hie;
        _generator = generator;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("DataSimulator is disabled (DataSimulator:Enabled=false); idling.");
            return;
        }

        _logger.LogInformation(
            "DataSimulator started: {PatientsPerTick} patient(s) every {Interval}s against EHR {Ehr}, HIS {His}, Lab {Lab}, HIE {Hie}.",
            _options.PatientsPerTick, _options.IntervalSeconds, _options.Modules.Ehr, _options.Modules.His, _options.Modules.Lab, _options.Modules.Hie);

        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.IntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            for (var i = 0; i < Math.Max(1, _options.PatientsPerTick); i++)
            {
                var patient = _generator.Generate(_options.Seed, Interlocked.Increment(ref _sequence));
                await RunJourneyAsync(patient, stoppingToken).ConfigureAwait(false);
            }

            try
            {
                await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunJourneyAsync(GeneratedPatient patient, CancellationToken cancellationToken)
    {
        try
        {
            var patientId = await _ehr.RegisterPatientAsync(patient, cancellationToken).ConfigureAwait(false);

            Guid? appointmentId = null;
            if (patient.Inpatient)
            {
                await _his.AdmitPatientAsync(patientId, patient.WardCode, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var slotStart = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
                appointmentId = await _his.BookAppointmentAsync(patientId, patient.ProviderId, slotStart, slotStart.AddMinutes(30), cancellationToken).ConfigureAwait(false);
            }

            var encounterId = await _ehr.StartEncounterAsync(
                patientId, patient.ProviderId, patient.Inpatient ? "IMP" : "AMB", appointmentId, cancellationToken).ConfigureAwait(false);

            var orderId = await _lab.PlaceLabOrderAsync(patientId, "Serum", cancellationToken).ConfigureAwait(false);

            var summary = Encoding.UTF8.GetBytes($"Visit summary for patient {patientId:N} (encounter {encounterId:N}).");
            var documentId = await _hie.UploadDocumentAsync(patientId, "VisitSummary", "Visit Summary", "text/plain", summary, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Journey complete: patient {PatientId} encounter {EncounterId} labOrder {OrderId} document {DocumentId} ({Shape}).",
                patientId, encounterId, orderId, documentId, patient.Inpatient ? "inpatient" : "outpatient");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Patient journey failed (MRN {Mrn}); continuing.", patient.MedicalRecordNumber);
        }
    }
}
