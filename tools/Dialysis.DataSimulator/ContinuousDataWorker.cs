using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
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
    // Keep a pool of live dialysis sessions so the chairside always has something to stream without
    // sessions growing without bound. Sized so each session stays in-progress long enough to show a
    // sustained 2s vitals waveform before it ages out: lifetime ≈ MaxLiveSessions / PatientsPerTick ×
    // IntervalSeconds (with the defaults below, ≈ 12 / 2 × 5 = 30s per chair). The vitals ticker caps
    // its per-tick fan-out, so this pool size never overwhelms the 2s cadence.
    private const int MaxLiveSessions = 12;

    // Cap the staff appointment-request worklist so it stays small and approvable. The journey files new
    // requests slowly (gated below), and the startup drain trims any backlog a persistent EHR DB accrued.
    private const int MaxPendingAppointmentRequests = 12;

    private readonly IEhrClient _ehr;
    private readonly IHisClient _his;
    private readonly ILabClient _lab;
    private readonly IHieClient _hie;
    private readonly IPdmsClient _pdms;
    private readonly ISmartConnectClient _smartConnect;
    private readonly DataSimulatorOptions _options;
    private readonly ILogger<ContinuousDataWorker> _logger;
    private long _sequence;

    // Captured once at the top of ExecuteAsync so the best-effort helpers (which don't take a token) can
    // tell a genuine shutdown from an HttpClient timeout — see CancellationClassifier for why that matters.
    private CancellationToken _stoppingToken;

    /// <summary>Creates the worker.</summary>
    public ContinuousDataWorker(
        IEhrClient ehr,
        IHisClient his,
        ILabClient lab,
        IHieClient hie,
        IPdmsClient pdms,
        ISmartConnectClient smartConnect,
        IOptions<DataSimulatorOptions> options,
        ILogger<ContinuousDataWorker> logger)
    {
        _ehr = ehr;
        _his = his;
        _lab = lab;
        _hie = hie;
        _pdms = pdms;
        _smartConnect = smartConnect;
        _options = options.Value;
        _logger = logger;

        // The module databases persist across AppHost restarts and the generator is deterministic, so a
        // counter that always starts at 0 would replay the same MRNs every run and collide with patients
        // already registered. Start from a random per-run offset so each run produces a fresh population.
        _sequence = _options.RandomizeSequenceStart
            ? Random.Shared.NextInt64(0, long.MaxValue - int.MaxValue)
            : 0;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;

        if (!_options.Enabled)
        {
            _logger.LogInformation("DataSimulator is disabled (DataSimulator:Enabled=false); idling.");
            return;
        }

        _logger.LogInformation(
            "DataSimulator started: {PatientsPerTick} patient(s) every {Interval}s against EHR {Ehr}, HIS {His}, Lab {Lab}, HIE {Hie}.",
            _options.PatientsPerTick, _options.IntervalSeconds, _options.Modules.Ehr, _options.Modules.His, _options.Modules.Lab, _options.Modules.Hie);

        // Registry-level demo data (TEFCA QHIN partners + MPI duplicate review queue) is seeded once at
        // startup — it isn't part of any single patient journey. Idempotent, so restarts don't pile up.
        // Best-effort like the rest: a slow/unreachable endpoint here must not abort startup or fault the
        // host (the journey loop below still runs and self-heals once the modules are reachable).
        await TryAsync("seed.hie-registry", () => SeedHieRegistryAsync(stoppingToken)).ConfigureAwait(false);
        // Admin / operational master data shown on the per-module admin consoles (reporting templates,
        // inventory, on-call, fee schedule, billing exports, terminology, alarm-dispatch audit).
        await TryAsync("seed.admin-registries", () => SeedAdminRegistriesAsync(stoppingToken)).ConfigureAwait(false);
        // SmartConnect integration flows — seeded separately because SmartConnect persistence is
        // in-memory (resets every restart), so this re-seeds each run rather than gating on the
        // persistent PDMS marker above.
        await TryAsync("seed.smartconnect-flows", () => SeedSmartConnectFlowsAsync(stoppingToken)).ConfigureAwait(false);
        // Drain any backlog of in-progress sessions left by prior runs down to MaxLiveSessions. The
        // PDMS DB persists across restarts, so without this the live pool grows unbounded and the
        // vitals ticker fans out across dozens of sessions — starving the chairside 2s cadence.
        await TryAsync("seed.drain-sessions", () => DrainExcessLiveSessionsAsync(stoppingToken)).ConfigureAwait(false);
        // Trim the staff appointment-request worklist down to a small, approvable size — clears the backlog
        // a long-lived EHR DB accumulated from earlier runs (the collision-prone "duplicate" pile).
        await TryAsync("seed.drain-appointment-requests", () => DrainExcessPendingAppointmentRequestsAsync(stoppingToken)).ConfigureAwait(false);

        var interval = TimeSpan.FromSeconds(Math.Max(1, _options.IntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            for (var i = 0; i < Math.Max(1, _options.PatientsPerTick); i++)
            {
                var patient = PatientGenerator.Generate(_options.Seed, Interlocked.Increment(ref _sequence));
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

    /// <summary>
    /// Completes the stalest in-progress sessions until the live pool is back down to
    /// <see cref="MaxLiveSessions"/>. ListInProgressSessions is oldest-first, so the leading entries
    /// are the longest-running — exactly the ones a clinic would have closed out long ago.
    /// </summary>
    private async Task DrainExcessLiveSessionsAsync(CancellationToken cancellationToken)
    {
        var live = await _pdms.ListInProgressSessionsAsync(cancellationToken).ConfigureAwait(false);
        var excess = live.Count - MaxLiveSessions;
        if (excess <= 0)
            return;

        _logger.LogInformation("Draining {Excess} backlogged in-progress session(s) down to {Max}.", excess, MaxLiveSessions);
        for (var i = 0; i < excess; i++)
        {
            var id = live[i].Id;
            await TryAsync("drain.session", () => _pdms.CompleteSessionAsync(id, 2.4m, cancellationToken)).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Trims the staff pending appointment-request queue back down to <see cref="MaxPendingAppointmentRequests"/>
    /// by cancelling the surplus, so a long-lived (persistent) EHR DB doesn't carry an unbounded,
    /// collision-prone worklist between runs. The pending list is soonest-preferred first, so we keep
    /// those and cancel the furthest-out tail.
    /// </summary>
    private async Task DrainExcessPendingAppointmentRequestsAsync(CancellationToken cancellationToken)
    {
        var pending = await _ehr.ListPendingAppointmentRequestsAsync(500, cancellationToken).ConfigureAwait(false);
        if (pending.Count <= MaxPendingAppointmentRequests)
            return;

        _logger.LogInformation("Draining {Excess} backlogged pending appointment request(s) down to {Max}.",
            pending.Count - MaxPendingAppointmentRequests, MaxPendingAppointmentRequests);
        for (var i = MaxPendingAppointmentRequests; i < pending.Count; i++)
        {
            var request = pending[i];
            await TryAsync("drain.appointment-request",
                () => _ehr.CancelAppointmentRequestAsync(request.PatientId, request.Id, cancellationToken)).ConfigureAwait(false);
        }
    }

    private async Task RunJourneyAsync(GeneratedPatient patient, CancellationToken cancellationToken)
    {
        // Register the patient first — every downstream call threads this id. A 409 means the MRN is
        // already taken (deterministic-seed collision against the persistent DB): skip the journey.
        Guid patientId;
        try
        {
            patientId = await _ehr.RegisterPatientAsync(patient, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogDebug(ex, "Patient already exists (MRN {Mrn}); skipping journey.", patient.MedicalRecordNumber);
            return;
        }
        catch (Exception ex) when (!CancellationClassifier.IsHostStopping(ex, _stoppingToken))
        {
            _logger.LogWarning(ex, "Patient registration failed (MRN {Mrn}); continuing.", patient.MedicalRecordNumber);
            return;
        }

        var provider = patient.ProviderId;

        // --- HIS: scheduling / patient-flow / orders / devices ---------------------------------
        Guid? appointmentId = null;
        if (patient.Inpatient)
        {
            await TryAsync("his.admit", () => _his.AdmitPatientAsync(patientId, patient.WardCode, cancellationToken)).ConfigureAwait(false);
        }
        else
        {
            var slotStart = DateTime.UtcNow.Date.AddDays(1).AddHours(9);
            appointmentId = await TryAsync("his.book-appointment",
                () => _his.BookAppointmentAsync(patientId, provider, slotStart, slotStart.AddMinutes(30), cancellationToken)).ConfigureAwait(false);
        }

        await TryAsync("his.queue", async () =>
        {
            var entryId = await _his.RegisterWalkInAsync($"{patient.GivenName} {patient.FamilyName}", patient.MedicalRecordNumber, cancellationToken).ConfigureAwait(false);
            // Chairs are a finite, per-day resource — spread assignments across the pool, and treat a
            // "chair occupied" rejection (a full clinic) as a benign Debug, not a journey failure.
            var chair = $"A{1 + (Math.Abs(patient.MedicalRecordNumber.GetHashCode(StringComparison.Ordinal)) % 20)}";
            try
            {
                await _his.AssignChairAsync(entryId, chair, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!CancellationClassifier.IsHostStopping(ex, _stoppingToken))
            {
                _logger.LogDebug(ex, "Chair {Chair} not assigned (likely occupied).", chair);
            }
        }).ConfigureAwait(false);
        await TryAsync("his.medication-order", () => _his.PlaceMedicationOrderAsync(patientId, cancellationToken)).ConfigureAwait(false);
        await TryAsync("his.device", async () =>
        {
            var deviceId = $"DEV-{patient.MedicalRecordNumber}";
            await _his.RegisterDeviceAsync(deviceId, cancellationToken).ConfigureAwait(false);
            await _his.IngestDeviceReadingAsync(deviceId, patientId, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        // --- EHR: encounter + chart + clinical orders + portal ---------------------------------
        var encounterId = await TryAsync("ehr.encounter",
            () => _ehr.StartEncounterAsync(patientId, provider, patient.Inpatient ? "IMP" : "AMB", appointmentId, cancellationToken)).ConfigureAwait(false);

        await TryAsync("ehr.vitals", () => _ehr.RecordVitalSignAsync(patientId, encounterId, provider, cancellationToken)).ConfigureAwait(false);
        await TryAsync("ehr.allergy", () => _ehr.RecordAllergyAsync(patientId, cancellationToken)).ConfigureAwait(false);
        await TryAsync("ehr.care-team", () => _ehr.AddCareTeamMemberAsync(patientId, provider, cancellationToken)).ConfigureAwait(false);
        await TryAsync("ehr.care-plan", async () =>
        {
            var carePlanId = await _ehr.CreateCarePlanAsync(patientId, provider, cancellationToken).ConfigureAwait(false);
            await _ehr.AddCarePlanGoalAsync(carePlanId, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        if (encounterId is { } enc)
        {
            await TryAsync("ehr.note", async () =>
            {
                var noteId = await _ehr.DraftNoteAsync(enc, patientId, provider, cancellationToken).ConfigureAwait(false);
                await _ehr.SignNoteAsync(noteId, provider, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
            await TryAsync("ehr.prescription", () => _ehr.OrderPrescriptionAsync(patientId, enc, provider, cancellationToken)).ConfigureAwait(false);
            await TryAsync("ehr.imaging", async () =>
            {
                var imagingOrderId = await _ehr.OrderImagingStudyAsync(patientId, enc, provider, cancellationToken).ConfigureAwait(false);
                // Close the result loop for ~half the orders so the chart shows a *completed* study, not
                // just a placed order — mirrors SmartConnect DICOM STOWing the fulfilled study back.
                if (Math.Abs(imagingOrderId.GetHashCode()) % 2 == 0)
                {
                    var studyUid = $"1.2.840.10008.5.1.4.1.1.{(uint)imagingOrderId.GetHashCode()}";
                    await _ehr.LinkImagingStudyAsync(imagingOrderId, studyUid, cancellationToken).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
            await TryAsync("ehr.avs", () => _ehr.AuthorAfterVisitSummaryAsync(patientId, enc, provider, cancellationToken)).ConfigureAwait(false);
        }

        await TryAsync("ehr.referral", () => _ehr.RequestReferralAsync(patientId, provider, cancellationToken)).ConfigureAwait(false);
        // Only ~1 in 8 patients files a portal appointment request — otherwise the staff worklist grows
        // without bound (one per patient, never auto-resolved). The startup drain trims any backlog.
        if ((patientId.GetHashCode() & 7) == 0)
            await TryAsync("ehr.portal-appointment", () => _ehr.RequestPortalAppointmentAsync(patientId, cancellationToken)).ConfigureAwait(false);
        await TryAsync("ehr.portal-message", () => _ehr.SendPortalMessageAsync(patientId, cancellationToken)).ConfigureAwait(false);

        // --- Lab (order through the EHR BFF's _x/lab aggregation; result back through EHR) ------
        await TryAsync("lab.order-result", async () =>
        {
            var labOrderId = await _lab.PlaceLabOrderAsync(patientId, "Serum", cancellationToken).ConfigureAwait(false);
            // Result ~half the orders (LOINC observations matching the two ordered tests) so the EHR
            // chart + portal lab panel show resulted labs, not perpetually-pending orders — mirrors an
            // ORU result returning from the LIS.
            if (Math.Abs(labOrderId.GetHashCode()) % 2 == 0)
            {
                await _ehr.IngestLabResultAsync(patientId, labOrderId, "718-7", "13.2", "g/dL", "13.5-17.5", "L", cancellationToken).ConfigureAwait(false);
                await _ehr.IngestLabResultAsync(patientId, labOrderId, "2160-0", "9.4", "mg/dL", "0.74-1.35", "H", cancellationToken).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);

        // --- HIE: document (+ sign) + consent --------------------------------------------------
        await TryAsync("hie.document", async () =>
        {
            var summary = VisitSummaryPdf.Render("Visit Summary", $"Visit summary for patient {patientId:N}.");
            var documentId = await _hie.UploadDocumentAsync(patientId, "VisitSummary", "Visit Summary", "application/pdf", summary, cancellationToken).ConfigureAwait(false);
            await _hie.SignDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
        await TryAsync("hie.consent", () => _hie.GrantConsentAsync(patientId, cancellationToken)).ConfigureAwait(false);

        // --- PDMS: start a session for this patient; complete an older one each tick so charges
        //     (EHR) + invoice documents (HIE) flow continuously, while keeping a live pool for the
        //     chairside vitals stream. Some completions log an intradialytic adverse event (→ EHR
        //     safety surveillance).
        var hash = Math.Abs(patient.MedicalRecordNumber.GetHashCode(StringComparison.Ordinal));
        Guid startedSession = Guid.Empty;
        await TryAsync("pdms.session", async () =>
        {
            var live = await _pdms.ListInProgressSessionsAsync(cancellationToken).ConfigureAwait(false);
            var sessionId = await _pdms.ScheduleSessionAsync(patientId, cancellationToken).ConfigureAwait(false);
            await _pdms.StartSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
            startedSession = sessionId;

            // Complete the oldest once the pool is full so charges (EHR) + invoice documents (HIE) flow
            // continuously. Keep the pool sized so each chair streams 2s vitals for a realistic stretch
            // before it ages out (lifetime ≈ MaxLiveSessions / PatientsPerTick × IntervalSeconds).
            if (live.Count >= MaxLiveSessions)
            {
                var finishing = live[0].Id;
                if (hash % 3 == 0)
                {
                    var (kind, severity) = AdverseEvents[hash % AdverseEvents.Length];
                    await _pdms.RecordAdverseEventAsync(finishing, kind, severity, "Observed during treatment.", cancellationToken).ConfigureAwait(false);
                }
                await _pdms.CompleteSessionAsync(finishing, 2.4m, cancellationToken).ConfigureAwait(false);
            }
        }).ConfigureAwait(false);

        // Drive the active-alarms board in its OWN step so a transient alarm failure can never skip the
        // session completion above (which would let the live pool grow unbounded). Idempotent —
        // resolve-or-refresh collapses repeats onto the same (machine, code) aggregate.
        if (startedSession != Guid.Empty)
        {
            await TryAsync("pdms.machine-alarm", () =>
            {
                var machine = MachineAlarms[hash % MachineAlarms.Length];
                var clear = hash % 4 == 0; // ~1 in 4 resolves instead of (re)raising
                return _pdms.RaiseMachineAlarmAsync(
                    machine.Serial, machine.Code, machine.Source, machine.Phase,
                    clear ? "Resolved" : "Present", startedSession, cancellationToken);
            }).ConfigureAwait(false);
        }

        _logger.LogInformation("Journey complete: patient {PatientId} ({Shape}).",
            patientId, patient.Inpatient ? "inpatient" : "outpatient");
    }

    // -------- One-time HIE registry seeding (TEFCA partners + MPI duplicate review queue) --------

    /// <summary>Seeds admin-surface demo data shown on the HIE consoles. Best-effort + idempotent.</summary>
    private async Task SeedHieRegistryAsync(CancellationToken cancellationToken)
    {
        await TryAsync("seed.tefca-partners", () => SeedTefcaPartnersAsync(cancellationToken)).ConfigureAwait(false);
        await TryAsync("seed.mpi-duplicates", () => SeedMpiDuplicatesAsync(cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>
    /// Onboards a few QHIN partners for <c>/hie/admin/tefca/partners</c>. Two are driven all the way to
    /// Active (self-signed trust anchor + mTLS material — activation requires both); one is left in
    /// Onboarding to show the lifecycle. Idempotent: partners already present (by name) are skipped.
    /// </summary>
    private async Task SeedTefcaPartnersAsync(CancellationToken cancellationToken)
    {
        var existing = await _hie.ListTefcaPartnersAsync(cancellationToken).ConfigureAwait(false);
        var existingNames = existing.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var desired = new[]
        {
            (Name: "Epic Nexus QHIN", Host: "nexus.epic.example", Activate: true),
            (Name: "CommonWell Health Alliance", Host: "qhin.commonwell.example", Activate: true),
            (Name: "eHealth Exchange QHIN", Host: "qhin.ehealthexchange.example", Activate: false),
        };

        var onboarded = 0;
        foreach (var partner in desired)
        {
            if (existingNames.Contains(partner.Name))
                continue;

            var id = await _hie.OnboardTefcaPartnerAsync(
                partner.Name,
                $"https://{partner.Host}/fhir",
                $"https://{partner.Host}/ias",
                cancellationToken).ConfigureAwait(false);
            onboarded++;

            if (partner.Activate)
            {
                var (pem, base64Pfx, pfxPassword) = GenerateSelfSignedMaterial(partner.Host);
                await _hie.AttachTrustAnchorAsync(id, pem, cancellationToken).ConfigureAwait(false);
                await _hie.RotateMtlsAsync(id, base64Pfx, pfxPassword, cancellationToken).ConfigureAwait(false);
                await _hie.TransitionPartnerStatusAsync(id, "Active", cancellationToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("TEFCA partner seed: {Onboarded} onboarded ({Existing} already present).",
            onboarded, existingNames.Count);
    }

    /// <summary>
    /// Feeds the MPI steward queue at <c>/hie/admin/mpi/reviews</c>: ingests the same person from two
    /// different partner sources via inbound FHIR. The duplicate-detection pass scores them as the same
    /// patient and queues a cross-source review. A wildcard inbound consent is granted first so the
    /// pushes clear the (fail-closed) consent gate. Idempotent: re-ingest refreshes the index entry and
    /// the review store dedups on the entry pair, so reviews don't pile up across restarts.
    /// </summary>
    private async Task SeedMpiDuplicatesAsync(CancellationToken cancellationToken)
    {
        const string scope = "patient.demographics";
        const string sourceA = "epic-nexus-qhin";
        const string sourceB = "commonwell-qhin";

        await _hie.GrantInboundConsentAsync(sourceA, scope, cancellationToken).ConfigureAwait(false);
        await _hie.GrantInboundConsentAsync(sourceB, scope, cancellationToken).ConfigureAwait(false);

        var duplicates = new (string Family, string Given, DateOnly Dob, string Mrn, string Gender)[]
        {
            ("Hawkins", "Margaret", new DateOnly(1948, 3, 12), "MPI-100481", "female"),
            ("Okafor", "Daniel", new DateOnly(1972, 11, 2), "MPI-205512", "male"),
            ("Petrov", "Irina", new DateOnly(1985, 6, 27), "MPI-309923", "female"),
            ("Nguyen", "Thomas", new DateOnly(1960, 1, 19), "MPI-441276", "male"),
        };

        for (var i = 0; i < duplicates.Length; i++)
        {
            var d = duplicates[i];
            // Same demographics, two sources → the second ingest scores Certain against the first and is
            // queued for a steward (auto-link is off by default, so cross-source certain matches still queue).
            await _hie.IngestInboundPatientAsync(sourceA, $"mpi-{sourceA}-{i}", d.Mrn, d.Family, d.Given, d.Dob, d.Gender, cancellationToken).ConfigureAwait(false);
            await _hie.IngestInboundPatientAsync(sourceB, $"mpi-{sourceB}-{i}", d.Mrn, d.Family, d.Given, d.Dob, d.Gender, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("MPI duplicate seed: ingested {Pairs} cross-source duplicate pair(s).", duplicates.Length);
    }

    /// <summary>Generates a self-signed cert and returns (PEM trust anchor, base64 PFX, PFX password) for partner activation.</summary>
    private static (string Pem, string Base64Pfx, string PfxPassword) GenerateSelfSignedMaterial(string commonName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest($"CN={commonName}", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(3));
        // Not a credential: throwaway passphrase for a self-signed PFX generated in-memory
        // for the demo TEFCA partner; the cert never leaves the simulator run.
#pragma warning disable S2068
        const string pfxPassword = "simulator-dev-pfx";
#pragma warning restore S2068
        var pem = certificate.ExportCertificatePem();
        var pfx = certificate.Export(X509ContentType.Pfx, pfxPassword);
        return (pem, Convert.ToBase64String(pfx), pfxPassword);
    }

    // -------- One-time admin / operational registry seeding (per-module admin consoles) ---------

    // A fixed chair the seeded on-call rotation covers — also the chair the seeded IV-pump alarm
    // targets, so the alarm-dispatch consumer (which needs an active rotation for the chair) fires.
    private static readonly Guid SeedChairId = new("a0a0a0a0-0000-4000-8000-000000000001");

    // Readable (kind, severity) pairs — EHR safety surveillance buckets by these strings.
    private static readonly (string Kind, string Severity)[] AdverseEvents =
    [
        ("Hypotension", "Moderate"),
        ("Muscle cramps", "Minor"),
        ("Nausea", "Minor"),
        ("Chest pain", "Critical"),
        ("Hypertension", "Moderate"),
    ];

    // Fixed pool of dialysis-machine serials backing the active-alarms board. Each machine carries
    // at most one live alarm (one code per machine), so the active set stays bounded (resolve-or-
    // refresh collapses repeats onto the same aggregate) and tracks the live chairs rather than
    // growing one-per-session forever.
    private static readonly (string Serial, long Code, string Source, string Phase)[] MachineAlarms =
    [
        ("DM-5008-01", 1001, "Venous pressure", "Treatment"),
        ("DM-5008-02", 1002, "Arterial pressure", "Treatment"),
        ("DM-5008-03", 1003, "Transmembrane pressure", "Treatment"),
        ("DM-5008-04", 1004, "Blood leak detector", "Treatment"),
        ("DM-5008-05", 1005, "Air bubble detector", "Treatment"),
        ("DM-5008-06", 1006, "Conductivity", "Treatment"),
    ];

    /// <summary>
    /// Seeds the admin/operational consoles once per database. Gated on an on-call rotation already
    /// existing (the PDMS/EHR DBs persist across restarts), so it doesn't pile up on re-runs.
    /// </summary>
    private async Task SeedAdminRegistriesAsync(CancellationToken cancellationToken)
    {
        // Resilient probe: PDMS may still be warming up when the simulator starts. Retry for up to
        // ~60s before giving up so a transient connection-refused doesn't skip the entire admin seed
        // (which is what previously left the billing/inventory/reporting consoles empty).
        bool? alreadySeeded = null;
        for (var attempt = 1; attempt <= 20 && alreadySeeded is null; attempt++)
        {
            try
            {
                alreadySeeded = await _pdms.HasOnCallRotationsAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!CancellationClassifier.IsHostStopping(ex, _stoppingToken))
            {
                _logger.LogInformation(ex, "Admin-seed probe not ready (attempt {Attempt}/20); retrying.", attempt);
                try
                { await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) { return; }
            }
        }
        if (alreadySeeded is null)
        {
            _logger.LogWarning("Admin-seed probe never became ready; skipping admin seed this run.");
            return;
        }
        if (alreadySeeded.Value)
        {
            _logger.LogInformation("Admin registries already seeded; skipping.");
            return;
        }

        // Independent master data.
        await TryAsync("seed.pdms-reporting", () => SeedReportingTemplatesAsync(cancellationToken)).ConfigureAwait(false);
        await TryAsync("seed.pdms-inventory", () => SeedInventoryAsync(cancellationToken)).ConfigureAwait(false);
        await TryAsync("seed.ehr-fee-schedule", () => SeedFeeScheduleAsync(cancellationToken)).ConfigureAwait(false);
        await TryAsync("seed.his-billing-exports", () => SeedBillingExportsAsync(cancellationToken)).ConfigureAwait(false);
        await TryAsync("seed.hie-terminology", () => SeedTerminologyAsync(cancellationToken)).ConfigureAwait(false);

        // A dedicated patient + live session backs the alarm-dispatch + surveillance seeds.
        Guid? seedSession = null;
        await TryAsync("seed.pdms-session", async () =>
        {
            var seedPatient = PatientGenerator.Generate(910_000, 1);
            var seedPatientId = await _ehr.RegisterPatientAsync(seedPatient, cancellationToken).ConfigureAwait(false);
            var sessionId = await _pdms.ScheduleSessionAsync(seedPatientId, cancellationToken).ConfigureAwait(false);
            await _pdms.StartSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
            seedSession = sessionId;
        }).ConfigureAwait(false);

        // Surveillance spike — a burst of the same (kind, severity) so EHR flags it.
        if (seedSession is { } spikeSession)
        {
            await TryAsync("seed.ehr-surveillance", async () =>
            {
                for (var i = 0; i < 5; i++)
                    await _pdms.RecordAdverseEventAsync(spikeSession, "Hypotension", "Critical", "Symptomatic intradialytic hypotension.", cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        // On-call rotation + escalation policy (seeded last — it's the idempotency marker).
        await TryAsync("seed.pdms-oncall", () => SeedOnCallAsync(cancellationToken)).ConfigureAwait(false);

        // IV-pump alarm → dispatch audit (needs the rotation above + a live session).
        if (seedSession is { } alarmSession)
            await TryAsync("seed.pdms-alarm", () => SeedAlarmDispatchAsync(alarmSession, cancellationToken)).ConfigureAwait(false);
    }

    private async Task SeedReportingTemplatesAsync(CancellationToken cancellationToken)
    {
        await _pdms.CreateReportTemplateAsync("discharge-letter", "DischargeLetter", "Dialysis discharge letter",
            "# Discharge summary\n\nPatient completed the prescribed treatment course.", "system-seed", "en-US", cancellationToken).ConfigureAwait(false);
        await _pdms.CreateReportTemplateAsync("shift-report", "ShiftReport", "Nursing shift report",
            "# Shift handover\n\n- Chairs in use\n- Open alarms\n- Pending tasks", "system-seed", "en-US", cancellationToken).ConfigureAwait(false);
        await _pdms.CreateReportTemplateAsync("billing-document", "BillingDocument", "Treatment billing document",
            "# Billing\n\nItemised charges for the dialysis session.", "system-seed", "en-US", cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedInventoryAsync(CancellationToken cancellationToken)
    {
        var rxnorm = "http://www.nlm.nih.gov/research/umls/rxnorm";
        var expiry = DateTime.UtcNow.AddYears(1);
        await _pdms.CreateInventoryItemAsync(rxnorm, "1361226", "Heparin sodium 5000 unit/mL injection", "LOT-HEP-2026A", expiry, 120, 20, cancellationToken).ConfigureAwait(false);
        await _pdms.CreateInventoryItemAsync(rxnorm, "1807634", "Epoetin alfa 4000 unit/mL injection", "LOT-EPO-2026A", expiry, 8, 10, cancellationToken).ConfigureAwait(false); // low stock
        await _pdms.CreateInventoryItemAsync(rxnorm, "313002", "Sodium chloride 0.9% 1000 mL", "LOT-NS-2026A", expiry, 300, 50, cancellationToken).ConfigureAwait(false);
        await _pdms.CreateInventoryItemAsync(rxnorm, "1719", "Calcium gluconate 100 mg/mL", "LOT-CA-2026A", expiry.AddMonths(-3), 45, 15, cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedFeeScheduleAsync(CancellationToken cancellationToken)
    {
        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-1));
        await _ehr.CreateFeeScheduleAsync("90935", "*", 235.00m, "USD", from, cancellationToken).ConfigureAwait(false);
        await _ehr.CreateFeeScheduleAsync("90937", "*", 290.00m, "USD", from, cancellationToken).ConfigureAwait(false);
        await _ehr.CreateFeeScheduleAsync("90945", "*", 210.00m, "USD", from, cancellationToken).ConfigureAwait(false);
        await _ehr.CreateFeeScheduleAsync("90999", "MEDICARE", 198.50m, "USD", from, cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedBillingExportsAsync(CancellationToken cancellationToken)
    {
        var monthStart = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        await _his.SubmitBillingExportJobAsync("MEDICARE", monthStart.AddMonths(-1), monthStart.AddDays(-1), "Prior-month Medicare claims", cancellationToken).ConfigureAwait(false);
        await _his.SubmitBillingExportJobAsync("AETNA", monthStart.AddMonths(-1), monthStart.AddDays(-1), "Prior-month commercial claims", cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedTerminologyAsync(CancellationToken cancellationToken)
    {
        const string csUrl = "https://dialysis.local/fhir/CodeSystem/modality";
        var codeSystem = JsonSerializer.Serialize(new
        {
            resourceType = "CodeSystem",
            url = csUrl,
            version = "1.0.0",
            status = "active",
            content = "complete",
            concept = new[]
            {
                new { code = "HD", display = "Hemodialysis" },
                new { code = "HDF", display = "Hemodiafiltration" },
                new { code = "PD", display = "Peritoneal dialysis" },
            },
        });
        await _hie.CreateTerminologyResourceAsync("CodeSystem", csUrl, "1.0.0", "active", "Dialysis modality", codeSystem, cancellationToken).ConfigureAwait(false);

        const string vsUrl = "https://dialysis.local/fhir/ValueSet/modality";
        var valueSet = JsonSerializer.Serialize(new
        {
            resourceType = "ValueSet",
            url = vsUrl,
            version = "1.0.0",
            status = "active",
            compose = new { include = new[] { new { system = csUrl } } },
        });
        await _hie.CreateTerminologyResourceAsync("ValueSet", vsUrl, "1.0.0", "active", "Dialysis modality value set", valueSet, cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedOnCallAsync(CancellationToken cancellationToken)
    {
        await _pdms.CreateEscalationPolicyAsync("Standard escalation", 120, 300, 300, 600, 900, quietHoursSuppressNonCritical: true, cancellationToken).ConfigureAwait(false);

        var from = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var until = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(3));
        await _pdms.CreateOnCallRotationAsync(SeedChairId, "morning", from, until,
            new OnCallChainSeed("clinician-amaya", "Dr. Amaya Okonkwo", "sms", "+15551230001"),
            new OnCallChainSeed("clinician-li", "Dr. Wei Li", "push.fcm", "device-token-li"),
            new OnCallChainSeed("charge-nurse", "Charge Nurse Desk", "voice", "+15551230009"),
            cancellationToken).ConfigureAwait(false);
        await _pdms.CreateOnCallRotationAsync(SeedChairId, "night", from, until,
            new OnCallChainSeed("clinician-rivera", "Dr. Sofia Rivera", "sms", "+15551230002"),
            new OnCallChainSeed("clinician-amaya", "Dr. Amaya Okonkwo", "email", "amaya@example.org"),
            new OnCallChainSeed("charge-nurse", "Charge Nurse Desk", "voice", "+15551230009"),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedSmartConnectFlowsAsync(CancellationToken cancellationToken)
    {
        if (await _smartConnect.HasFlowsAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation("SmartConnect flows already present; skipping flow seed.");
            return;
        }
        await _smartConnect.CreateFlowAsync("ADT Inbound (HL7 v2)", "Receives ADT admit/discharge/transfer feeds from the HIS.", "HL7v2", started: true, cancellationToken).ConfigureAwait(false);
        await _smartConnect.CreateFlowAsync("ORU Lab Results (HL7 v2)", "Ingests ORU result messages from the laboratory.", "HL7v2", started: true, cancellationToken).ConfigureAwait(false);
        await _smartConnect.CreateFlowAsync("FHIR Bundle Bridge", "Transforms inbound HL7 v2 to FHIR R4 bundles for the HIE.", "FHIR", started: false, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("SmartConnect flow seed: created 3 integration flows.");
    }

    private async Task SeedAlarmDispatchAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        const string deviceId = "SIGMA-SEED-PUMP-1";
        var now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        // Start the infusion (lazily created), then raise an alarm — the OnCall consumer turns the
        // alarm into an AlarmDispatch for the chair (the seeded rotation covers SeedChairId).
        await _pdms.IngestIvPumpTelemetryAsync("baxter-sigma", sessionId, SeedChairId, new
        {
            deviceId,
            eventType = "INFUSION_START",
            seq = 1,
            ts = now,
            rate = new { programmedMlPerHr = 100.0, actualMlPerHr = 100.0 },
            volume = new { programmedMl = 250.0, infusedMl = 0.0 },
            drug = new { atc = "B01AB01", name = "Heparin" },
        }, cancellationToken).ConfigureAwait(false);
        await _pdms.IngestIvPumpTelemetryAsync("baxter-sigma", sessionId, SeedChairId, new
        {
            deviceId,
            eventType = "ALARM",
            seq = 2,
            ts = DateTime.UtcNow.AddSeconds(5).ToString("o", CultureInfo.InvariantCulture),
            alarm = new { code = "OCCLUSION", text = "Downstream occlusion detected", severity = "WARNING" },
        }, cancellationToken).ConfigureAwait(false);

        // Seed two live machine treatment alarms so the active-alarms board is non-empty the moment
        // the stack comes up, before the per-patient journey has raised any of its own.
        await _pdms.RaiseMachineAlarmAsync(MachineAlarms[0].Serial, MachineAlarms[0].Code,
            MachineAlarms[0].Source, MachineAlarms[0].Phase, "Present", sessionId, cancellationToken).ConfigureAwait(false);
        await _pdms.RaiseMachineAlarmAsync(MachineAlarms[2].Serial, MachineAlarms[2].Code,
            MachineAlarms[2].Source, MachineAlarms[2].Phase, "Present", sessionId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Runs one best-effort journey step; a failure is logged concisely and swallowed so the
    /// rest of the journey continues (one unpopulated endpoint must not abort the whole patient).</summary>
    private async Task TryAsync(string step, Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (Exception ex) when (!CancellationClassifier.IsHostStopping(ex, _stoppingToken))
        {
            _logger.LogWarning(ex, "Journey step {Step} failed.", step);
        }
    }

    /// <summary>Best-effort step that yields an id to thread downstream; null when the step failed.</summary>
    private async Task<Guid?> TryAsync(string step, Func<Task<Guid>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (Exception ex) when (!CancellationClassifier.IsHostStopping(ex, _stoppingToken))
        {
            _logger.LogWarning(ex, "Journey step {Step} failed.", step);
            return null;
        }
    }
}
