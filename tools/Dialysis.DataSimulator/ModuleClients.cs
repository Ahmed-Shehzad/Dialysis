namespace Dialysis.DataSimulator;

/// <summary>EHR clinical + chart + portal write surface (driven through the EHR BFF).</summary>
public interface IEhrClient
{
    /// <summary>Registers a patient; returns the EHR patient id.</summary>
    Task<Guid> RegisterPatientAsync(GeneratedPatient patient, CancellationToken cancellationToken);

    /// <summary>Starts an encounter; returns the encounter id.</summary>
    Task<Guid> StartEncounterAsync(Guid patientId, Guid providerId, string encounterClassCode, Guid? appointmentId, CancellationToken cancellationToken);

    /// <summary>Records a vital sign on the chart.</summary>
    Task RecordVitalSignAsync(Guid patientId, Guid? encounterId, Guid providerId, CancellationToken cancellationToken);

    /// <summary>Records an allergy on the chart.</summary>
    Task RecordAllergyAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Creates a care plan; returns the care-plan id.</summary>
    Task<Guid> CreateCarePlanAsync(Guid patientId, Guid providerId, CancellationToken cancellationToken);

    /// <summary>Adds a goal to a care plan.</summary>
    Task AddCarePlanGoalAsync(Guid carePlanId, CancellationToken cancellationToken);

    /// <summary>Adds a provider to the patient's care team.</summary>
    Task AddCareTeamMemberAsync(Guid patientId, Guid providerId, CancellationToken cancellationToken);

    /// <summary>Drafts a clinical note; returns the note id.</summary>
    Task<Guid> DraftNoteAsync(Guid encounterId, Guid patientId, Guid providerId, CancellationToken cancellationToken);

    /// <summary>Signs a clinical note.</summary>
    Task SignNoteAsync(Guid noteId, Guid providerId, CancellationToken cancellationToken);

    /// <summary>Orders an outpatient prescription.</summary>
    Task OrderPrescriptionAsync(Guid patientId, Guid encounterId, Guid providerId, CancellationToken cancellationToken);

    /// <summary>Orders an imaging study.</summary>
    Task OrderImagingStudyAsync(Guid patientId, Guid encounterId, Guid providerId, CancellationToken cancellationToken);

    /// <summary>Requests an outbound referral.</summary>
    Task RequestReferralAsync(Guid patientId, Guid providerId, CancellationToken cancellationToken);

    /// <summary>Authors + publishes an after-visit summary for the encounter.</summary>
    Task AuthorAfterVisitSummaryAsync(Guid patientId, Guid encounterId, Guid providerId, CancellationToken cancellationToken);

    /// <summary>Files a patient-portal appointment request (staff-impersonated in dev).</summary>
    Task RequestPortalAppointmentAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Sends a patient-portal secure message (staff-impersonated in dev).</summary>
    Task SendPortalMessageAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Upserts a CPT fee-schedule rate (admin master data for the billing console).</summary>
    Task CreateFeeScheduleAsync(string cptCode, string payerCode, decimal amount, string currencyCode, DateOnly effectiveFromUtc, CancellationToken cancellationToken);
}

/// <summary>HIS scheduling + patient-flow + medication + device write surface (driven through the HIS BFF).</summary>
public interface IHisClient
{
    /// <summary>Books an appointment; returns the appointment id.</summary>
    Task<Guid> BookAppointmentAsync(Guid patientId, Guid providerId, DateTime slotStartUtc, DateTime slotEndUtc, CancellationToken cancellationToken);

    /// <summary>Admits a patient to a ward; returns the admission id.</summary>
    Task<Guid> AdmitPatientAsync(Guid patientId, string wardCode, CancellationToken cancellationToken);

    /// <summary>Registers a walk-in queue entry; returns the entry id.</summary>
    Task<Guid> RegisterWalkInAsync(string patientName, string mrn, CancellationToken cancellationToken);

    /// <summary>Assigns a chair to a queue entry.</summary>
    Task AssignChairAsync(Guid entryId, string chair, CancellationToken cancellationToken);

    /// <summary>Places a medication order.</summary>
    Task PlaceMedicationOrderAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Registers an integration device under the given external id.</summary>
    Task RegisterDeviceAsync(string deviceId, CancellationToken cancellationToken);

    /// <summary>Ingests one device reading for the registered device + patient.</summary>
    Task IngestDeviceReadingAsync(string deviceId, Guid patientId, CancellationToken cancellationToken);

    /// <summary>Queues a payer billing-export job (admin operations console).</summary>
    Task SubmitBillingExportJobAsync(string payerCode, DateOnly periodStart, DateOnly periodEnd, string? notes, CancellationToken cancellationToken);
}

/// <summary>Lab order write surface (driven through the EHR BFF's _x/lab aggregation).</summary>
public interface ILabClient
{
    /// <summary>Places a lab order; returns the order id.</summary>
    Task<Guid> PlaceLabOrderAsync(Guid patientId, string? specimen, CancellationToken cancellationToken);
}

/// <summary>HIE document + consent write surface (driven through the HIE BFF).</summary>
public interface IHieClient
{
    /// <summary>Uploads a document; returns the document id.</summary>
    Task<Guid> UploadDocumentAsync(Guid patientId, string kind, string title, string mimeType, byte[] content, CancellationToken cancellationToken);

    /// <summary>Applies a platform PAdES signature to a (PDF) document.</summary>
    Task SignDocumentAsync(Guid documentId, CancellationToken cancellationToken);

    /// <summary>Grants an outbound sharing consent for the patient.</summary>
    Task GrantConsentAsync(Guid patientId, CancellationToken cancellationToken);

    // --- TEFCA QHIN partner registry (admin surface) ------------------------------------------

    /// <summary>Lists onboarded QHIN partners (used to keep partner seeding idempotent across restarts).</summary>
    Task<IReadOnlyList<TefcaPartnerRow>> ListTefcaPartnersAsync(CancellationToken cancellationToken);

    /// <summary>Onboards a QHIN partner; returns the partner id.</summary>
    Task<Guid> OnboardTefcaPartnerAsync(string name, string fhirBaseUrl, string iasEndpoint, CancellationToken cancellationToken);

    /// <summary>Attaches a PEM X.509 trust anchor to a partner; returns the anchor id.</summary>
    Task<Guid> AttachTrustAnchorAsync(Guid partnerId, string certificatePem, CancellationToken cancellationToken);

    /// <summary>Uploads partner mTLS material (base64 PFX + password).</summary>
    Task RotateMtlsAsync(Guid partnerId, string base64Pfx, string pfxPassword, CancellationToken cancellationToken);

    /// <summary>Transitions a partner's lifecycle status (e.g. <c>Active</c>).</summary>
    Task TransitionPartnerStatusAsync(Guid partnerId, string nextStatus, CancellationToken cancellationToken);

    // --- Inbound FHIR + MPI duplicate seeding -------------------------------------------------

    /// <summary>Grants a wildcard (un-matched) inbound consent for a partner + scope, so inbound writes pass the consent gate.</summary>
    Task GrantInboundConsentAsync(string partnerId, string scope, CancellationToken cancellationToken);

    /// <summary>Pushes an inbound FHIR Patient from a partner (feeds the MPI duplicate-detection pass).</summary>
    Task IngestInboundPatientAsync(
        string partnerId, string logicalId, string mrn, string family, string given, DateOnly dateOfBirth, string gender, CancellationToken cancellationToken);

    /// <summary>Authors a FHIR terminology resource (CodeSystem / ValueSet / ConceptMap) for the terminology console.</summary>
    Task CreateTerminologyResourceAsync(string resourceType, string url, string version, string status, string name, string fhirJson, CancellationToken cancellationToken);
}

/// <summary>A QHIN partner registry row (subset used for idempotent seeding).</summary>
public sealed record TefcaPartnerRow(Guid Id, string Name, string Status);

/// <summary>SmartConnect integration-flow write surface (driven through the SmartConnect BFF).</summary>
public interface ISmartConnectClient
{
    /// <summary>True if any integration flow exists (idempotency probe; SmartConnect is in-memory so this resets each restart).</summary>
    Task<bool> HasFlowsAsync(CancellationToken cancellationToken);

    /// <summary>Creates an integration flow/channel with a minimal pass-through pipeline.</summary>
    Task CreateFlowAsync(string name, string description, string dataType, bool started, CancellationToken cancellationToken);
}

/// <summary>One in-progress session the vitals ticker feeds.</summary>
public sealed record PdmsSessionRow(Guid Id, string Status);

/// <summary>PDMS treatment-session write surface — drives live intradialytic telemetry.</summary>
public interface IPdmsClient
{
    /// <summary>Schedules a dialysis session for the patient; returns the session id.</summary>
    Task<Guid> ScheduleSessionAsync(Guid patientId, CancellationToken cancellationToken);

    /// <summary>Transitions a scheduled session to in-progress.</summary>
    Task StartSessionAsync(Guid sessionId, CancellationToken cancellationToken);

    /// <summary>Lists the currently in-progress sessions (so the ticker can feed each one).</summary>
    Task<IReadOnlyList<PdmsSessionRow>> ListInProgressSessionsAsync(CancellationToken cancellationToken);

    /// <summary>Records one intradialytic reading; the handler broadcasts it over the vitals hub.</summary>
    Task RecordReadingAsync(Guid sessionId, object reading, CancellationToken cancellationToken);

    /// <summary>Completes an in-progress session — cascades to the EHR charge + HIE invoice document.</summary>
    Task CompleteSessionAsync(Guid sessionId, decimal achievedUfVolumeLiters, CancellationToken cancellationToken);

    /// <summary>Records an intradialytic adverse event (feeds the EHR safety-surveillance read model).</summary>
    Task RecordAdverseEventAsync(Guid sessionId, string eventKindCode, string severity, string? notes, CancellationToken cancellationToken);

    /// <summary>Creates a reporting template version (reporting-templates admin page).</summary>
    Task CreateReportTemplateAsync(string slug, string kind, string title, string bodyMarkdown, string authoredBySub, string? languageCode, CancellationToken cancellationToken);

    /// <summary>Creates an on-call rotation for a chair (on-call rotation admin page).</summary>
    Task CreateOnCallRotationAsync(Guid chairId, string shiftCode, DateOnly effectiveFromUtc, DateOnly effectiveUntilUtc,
        OnCallChainSeed primary, OnCallChainSeed backup, OnCallChainSeed supervisor, CancellationToken cancellationToken);

    /// <summary>Creates an escalation policy (escalation-policy admin page).</summary>
    Task CreateEscalationPolicyAsync(string name, int criticalPrimarySeconds, int criticalBackupSeconds,
        int warningPrimarySeconds, int warningBackupSeconds, int informationalPrimarySeconds, bool quietHoursSuppressNonCritical, CancellationToken cancellationToken);

    /// <summary>Registers a medication inventory row (inventory admin page).</summary>
    Task CreateInventoryItemAsync(string medicationCodeSystem, string medicationCode, string medicationDisplay,
        string lotNumber, DateTime expiryUtc, int initialOnHandUnits, int threshold, CancellationToken cancellationToken);

    /// <summary>Posts raw IV-pump telemetry for a session+chair (drives the alarm-dispatch audit when Kind=Alarm).</summary>
    Task IngestIvPumpTelemetryAsync(string vendor, Guid sessionId, Guid chairId, object payload, CancellationToken cancellationToken);

    /// <summary>True if any on-call rotation exists — used as the idempotency probe for the one-time admin seed.</summary>
    Task<bool> HasOnCallRotationsAsync(CancellationToken cancellationToken);
}

/// <summary>One link in an on-call chain (single notification channel, for seeding).</summary>
public sealed record OnCallChainSeed(string ClinicianSub, string DisplayName, string Channel, string Address);

/// <summary>Typed EHR client.</summary>
public sealed class EhrClient : IEhrClient
{
    private readonly HttpClient _client;

    /// <summary>Creates the client.</summary>
    public EhrClient(HttpClient client) => _client = client;

    /// <inheritdoc />
    public Task<Guid> RegisterPatientAsync(GeneratedPatient patient, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patient);
        return HttpJson.PostReadIdAsync(_client, "api/v1.0/clinical/patients",
            new
            {
                patient.MedicalRecordNumber,
                patient.FamilyName,
                patient.GivenName,
                patient.DateOfBirth,
                patient.SexAtBirthCode,
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Guid> StartEncounterAsync(Guid patientId, Guid providerId, string encounterClassCode, Guid? appointmentId, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/clinical/encounters",
            new { patientId, providerId, encounterClassCode, appointmentId }, cancellationToken);

    /// <inheritdoc />
    public Task RecordVitalSignAsync(Guid patientId, Guid? encounterId, Guid providerId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/patient-chart/vitals",
            new
            {
                patientId,
                encounterId,
                loincCode = "8480-6",
                display = "Systolic blood pressure",
                value = 132m,
                unitCode = "mm[Hg]",
                observedAtUtc = DateTime.UtcNow,
                recordedByProviderId = providerId,
            },
            cancellationToken);

    /// <inheritdoc />
    public Task RecordAllergyAsync(Guid patientId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/patient-chart/allergies",
            new
            {
                patientId,
                allergenSystem = "http://snomed.info/sct",
                allergenCode = "227493005",
                allergenDisplay = "Cashew nuts",
                severity = "Moderate",
                verificationStatus = "Confirmed",
                reactionText = "Urticaria",
            },
            cancellationToken);

    /// <inheritdoc />
    public Task<Guid> CreateCarePlanAsync(Guid patientId, Guid providerId, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/care-plans",
            new { patientId, title = "Dialysis care plan", authoredByProviderId = providerId }, cancellationToken);

    /// <inheritdoc />
    public Task AddCarePlanGoalAsync(Guid carePlanId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, $"api/v1.0/care-plans/{carePlanId}/goals",
            new { description = "Maintain target dry weight", targetMeasure = "UF 2.5 L/session" }, cancellationToken);

    /// <inheritdoc />
    public Task AddCareTeamMemberAsync(Guid patientId, Guid providerId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, $"api/v1.0/care-team/patients/{patientId}/members",
            new { providerId, role = "PrimaryNephrologist", isPrimary = true }, cancellationToken);

    /// <inheritdoc />
    public Task<Guid> DraftNoteAsync(Guid encounterId, Guid patientId, Guid providerId, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/clinical/notes/draft",
            new
            {
                encounterId,
                patientId,
                authoringProviderId = providerId,
                subjective = "Patient reports stable energy between sessions.",
                objective = "BP 132/78, weight at target, access patent.",
                assessment = "ESRD on maintenance hemodialysis, stable.",
                plan = "Continue current prescription; recheck labs next visit.",
            },
            cancellationToken);

    /// <inheritdoc />
    public Task SignNoteAsync(Guid noteId, Guid providerId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, $"api/v1.0/clinical/notes/{noteId}/sign",
            new { signingProviderId = providerId }, cancellationToken);

    /// <inheritdoc />
    public Task OrderPrescriptionAsync(Guid patientId, Guid encounterId, Guid providerId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/clinical/prescriptions",
            new
            {
                patientId,
                encounterId,
                prescribingProviderId = providerId,
                medicationRxnormCode = "310965",
                medicationDisplay = "Ibuprofen 200 mg oral tablet",
                doseText = "200 mg",
                frequencyText = "TID",
                quantityDispensed = 30,
                refillsAuthorized = 2,
                pharmacyNcpdpId = "1234567",
                acknowledgeAdvisories = true,
            },
            cancellationToken);

    /// <inheritdoc />
    public Task OrderImagingStudyAsync(Guid patientId, Guid encounterId, Guid providerId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/clinical/imaging-orders",
            new
            {
                patientId,
                encounterId,
                orderingProviderId = providerId,
                modalityCode = "US",
                bodySiteCode = "RUQ",
                reasonText = "Vascular access surveillance",
            },
            cancellationToken);

    /// <inheritdoc />
    public Task RequestReferralAsync(Guid patientId, Guid providerId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/clinical/referrals",
            new
            {
                patientId,
                destinationPartnerId = "partner-cardiology",
                referringProviderId = providerId,
                referralReason = "Cardiology evaluation for fluid management",
            },
            cancellationToken);

    /// <inheritdoc />
    public async Task AuthorAfterVisitSummaryAsync(Guid patientId, Guid encounterId, Guid providerId, CancellationToken cancellationToken)
    {
        var summaryId = await HttpJson.PostReadIdAsync(_client, "api/v1.0/after-visit-summaries",
            new
            {
                patientId,
                encounterRef = encounterId,
                visitDateUtc = DateTime.UtcNow,
                authoringProviderId = providerId,
                narrative = "Today's dialysis session completed without complications.",
            },
            cancellationToken).ConfigureAwait(false);
        await HttpJson.PostAsync(_client, $"api/v1.0/after-visit-summaries/{summaryId}/lines",
            new { kind = "Instruction", text = "Keep your access site clean and dry." }, cancellationToken).ConfigureAwait(false);
        await HttpJson.PostAsync(_client, $"api/v1.0/after-visit-summaries/{summaryId}/lines",
            new { kind = "FollowUp", text = "Return for your next scheduled session." }, cancellationToken).ConfigureAwait(false);
        await HttpJson.PostAsync(_client, $"api/v1.0/after-visit-summaries/{summaryId}/publish", null, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task RequestPortalAppointmentAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var earliest = DateTime.UtcNow.Date.AddDays(3).AddHours(9);
        return HttpJson.PostAsync(_client, $"api/v1.0/portal/appointment-requests/patients/{patientId}",
            new
            {
                reasonText = "Routine follow-up",
                earliestPreferredUtc = earliest,
                latestPreferredUtc = earliest.AddDays(7),
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task SendPortalMessageAsync(Guid patientId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, $"api/v1.0/portal/messages/patients/{patientId}",
            new { subject = "Question about my medication", body = "Should I take my phosphate binder with meals?" },
            cancellationToken);

    /// <inheritdoc />
    public Task CreateFeeScheduleAsync(string cptCode, string payerCode, decimal amount, string currencyCode, DateOnly effectiveFromUtc, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/billing/fee-schedule",
            new { cptCode, payerCode, amount, currencyCode, effectiveFromUtc, effectiveUntilUtc = (DateOnly?)null },
            cancellationToken);
}

/// <summary>Typed HIS client.</summary>
public sealed class HisClient : IHisClient
{
    private readonly HttpClient _client;

    /// <summary>Creates the client.</summary>
    public HisClient(HttpClient client) => _client = client;

    /// <inheritdoc />
    public Task<Guid> BookAppointmentAsync(Guid patientId, Guid providerId, DateTime slotStartUtc, DateTime slotEndUtc, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/scheduling/appointments",
            new { patientId, providerId, slotStartUtc, slotEndUtc }, cancellationToken);

    /// <inheritdoc />
    public Task<Guid> AdmitPatientAsync(Guid patientId, string wardCode, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/patient-flow/admissions",
            new { patientId, wardCode }, cancellationToken);

    /// <inheritdoc />
    public Task<Guid> RegisterWalkInAsync(string patientName, string mrn, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/patient-flow/queue/walk-in",
            new { patientName, mrn, eligibilityVerified = true }, cancellationToken, "entryId", "id");

    /// <inheritdoc />
    public Task AssignChairAsync(Guid entryId, string chair, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/patient-flow/queue/assign-chair",
            new { entryId, chair }, cancellationToken);

    /// <inheritdoc />
    public Task PlaceMedicationOrderAsync(Guid patientId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/medication/orders",
            new { patientId, drugCode = "N07BA01", dosage = "1 tablet daily" }, cancellationToken);

    /// <inheritdoc />
    public Task RegisterDeviceAsync(string deviceId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/integration/devices",
            new
            {
                deviceId,
                deviceTypeCode = "dialysis-machine",
                manufacturer = "B. Braun",
                model = "Dialog iQ",
                serialNumber = deviceId,
            },
            cancellationToken);

    /// <inheritdoc />
    public Task IngestDeviceReadingAsync(string deviceId, Guid patientId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/integration/device-readings",
            new
            {
                deviceId,
                patientId,
                payloadJson = "{\"arterialPressureMmHg\":-180,\"venousPressureMmHg\":140,\"bloodFlowMlMin\":350}",
            },
            cancellationToken);

    /// <inheritdoc />
    public Task SubmitBillingExportJobAsync(string payerCode, DateOnly periodStart, DateOnly periodEnd, string? notes, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/operations/billing/export-jobs",
            new { payerCode, periodStart, periodEnd, notes },
            cancellationToken);
}

/// <summary>Typed Lab client.</summary>
public sealed class LabClient : ILabClient
{
    private readonly HttpClient _client;

    /// <summary>Creates the client.</summary>
    public LabClient(HttpClient client) => _client = client;

    /// <inheritdoc />
    public Task<Guid> PlaceLabOrderAsync(Guid patientId, string? specimen, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/lab/orders",
            new
            {
                patientId,
                tests = new[]
                {
                    new { loincCode = "718-7", display = "Hemoglobin" },
                    new { loincCode = "2160-0", display = "Creatinine" },
                },
                priority = "Routine",
                specimen,
            },
            cancellationToken);
}

/// <summary>Typed HIE client.</summary>
public sealed class HieClient : IHieClient
{
    private readonly HttpClient _client;

    /// <summary>Creates the client.</summary>
    public HieClient(HttpClient client) => _client = client;

    /// <inheritdoc />
    public Task<Guid> UploadDocumentAsync(Guid patientId, string kind, string title, string mimeType, byte[] content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        return HttpJson.PostReadIdAsync(_client, "api/v1.0/documents",
            new
            {
                patientId,
                kind,
                title,
                mimeType,
                base64Content = Convert.ToBase64String(content),
            },
            cancellationToken, "documentId", "id");
    }

    /// <inheritdoc />
    public Task SignDocumentAsync(Guid documentId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, $"api/v1.0/documents/{documentId}/sign",
            new { certificateSource = "Platform", reason = "Clinician sign-off", location = "Dialysis Clinic" },
            cancellationToken);

    /// <inheritdoc />
    public Task GrantConsentAsync(Guid patientId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/hie/consents",
            new
            {
                patientId,
                partnerId = "default",
                scope = "Demographics",
                direction = "Outbound",
                effectiveFromUtc = DateTime.UtcNow.AddMinutes(-1),
            },
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TefcaPartnerRow>> ListTefcaPartnersAsync(CancellationToken cancellationToken)
    {
        var rows = await HttpJson.GetEnvelopedAsync<List<TefcaPartnerRow>>(
            _client, "api/v1.0/tefca/partners", cancellationToken).ConfigureAwait(false);
        return rows ?? [];
    }

    /// <inheritdoc />
    public Task<Guid> OnboardTefcaPartnerAsync(string name, string fhirBaseUrl, string iasEndpoint, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/tefca/partners",
            new { name, fhirBaseUrl, iasEndpoint },
            cancellationToken, "id");

    /// <inheritdoc />
    public Task<Guid> AttachTrustAnchorAsync(Guid partnerId, string certificatePem, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, $"api/v1.0/tefca/partners/{partnerId}/trust-anchors",
            new { certificatePem },
            cancellationToken, "anchorId", "id");

    /// <inheritdoc />
    public Task RotateMtlsAsync(Guid partnerId, string base64Pfx, string pfxPassword, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, $"api/v1.0/tefca/partners/{partnerId}/mtls",
            new { base64Pfx, pfxPassword },
            cancellationToken);

    /// <inheritdoc />
    public Task TransitionPartnerStatusAsync(Guid partnerId, string nextStatus, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, $"api/v1.0/tefca/partners/{partnerId}/status",
            new { next = nextStatus },
            cancellationToken);

    /// <inheritdoc />
    public Task GrantInboundConsentAsync(string partnerId, string scope, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/hie/consents",
            new
            {
                // A wildcard inbound consent (PatientId = Guid.Empty) lets a not-yet-matched partner's
                // pushes pass the inbound consent gate — see EfConsentRepository.FindActiveByExternalReferenceAsync.
                patientId = Guid.Empty,
                partnerId,
                scope,
                direction = "Inbound",
                effectiveFromUtc = DateTime.UtcNow.AddMinutes(-1),
            },
            cancellationToken);

    /// <inheritdoc />
    public Task IngestInboundPatientAsync(
        string partnerId, string logicalId, string mrn, string family, string given, DateOnly dateOfBirth, string gender, CancellationToken cancellationToken) =>
        HttpJson.PostWithHeadersAsync(_client, "api/v1.0/fhir/Bundle",
            new
            {
                resourceType = "Patient",
                id = logicalId,
                identifier = new[]
                {
                    new { system = "http://terminology.hl7.org/CodeSystem/v2-0203", value = mrn },
                },
                name = new[]
                {
                    new { family, given = new[] { given } },
                },
                gender,
                birthDate = dateOfBirth.ToString("yyyy-MM-dd"),
            },
            new Dictionary<string, string> { ["X-HIE-Partner"] = partnerId },
            cancellationToken);

    /// <inheritdoc />
    public Task CreateTerminologyResourceAsync(string resourceType, string url, string version, string status, string name, string fhirJson, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/terminology/resources",
            new { resourceType, url, version, status, name, fhirJson },
            cancellationToken);
}

/// <summary>Typed PDMS client.</summary>
public sealed class PdmsClient : IPdmsClient
{
    private readonly HttpClient _client;

    /// <summary>Creates the client.</summary>
    public PdmsClient(HttpClient client) => _client = client;

    /// <inheritdoc />
    public Task<Guid> ScheduleSessionAsync(Guid patientId, CancellationToken cancellationToken) =>
        HttpJson.PostReadIdAsync(_client, "api/v1.0/sessions",
            new
            {
                patientId,
                scheduledStartUtc = DateTime.UtcNow,
                dialyzerModel = "FX CorDiax 80",
                prescribedDurationMinutes = 240,
                bloodFlowRateMlPerMin = 350,
                dialysateFlowRateMlPerMin = 500,
                dialysatePotassiumMmolPerL = 2.0m,
                dialysateCalciumMmolPerL = 1.5m,
                dialysateSodiumMmolPerL = 138m,
                targetUfVolumeLiters = 2.5m,
                anticoagulationProtocolCode = "HEPARIN-STD",
                accessKind = "ArteriovenousFistula",
                accessSite = "Left radiocephalic",
                accessEstablishedOn = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-1)),
            },
            cancellationToken);

    /// <inheritdoc />
    public Task StartSessionAsync(Guid sessionId, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, $"api/v1.0/sessions/{sessionId}/start", null, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PdmsSessionRow>> ListInProgressSessionsAsync(CancellationToken cancellationToken)
    {
        var rows = await HttpJson.GetAsync<List<PdmsSessionRow>>(
            _client, "api/v1.0/sessions?activeOnly=true&take=200", cancellationToken).ConfigureAwait(false);
        return (rows ?? [])
            .Where(r => string.Equals(r.Status, "InProgress", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <inheritdoc />
    public Task RecordReadingAsync(Guid sessionId, object reading, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, $"api/v1.0/sessions/{sessionId}/readings", reading, cancellationToken);

    /// <inheritdoc />
    public Task CompleteSessionAsync(Guid sessionId, decimal achievedUfVolumeLiters, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, $"api/v1.0/sessions/{sessionId}/complete",
            new { achievedUfVolumeLiters }, cancellationToken);

    /// <inheritdoc />
    public Task RecordAdverseEventAsync(Guid sessionId, string eventKindCode, string severity, string? notes, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, $"api/v1.0/sessions/{sessionId}/adverse-events",
            new { eventKindCode, severity, notes }, cancellationToken);

    /// <inheritdoc />
    public Task CreateReportTemplateAsync(string slug, string kind, string title, string bodyMarkdown, string authoredBySub, string? languageCode, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/reporting/templates",
            new { slug, kind, title, bodyMarkdown, authoredBySub, languageCode }, cancellationToken);

    /// <inheritdoc />
    public Task CreateOnCallRotationAsync(Guid chairId, string shiftCode, DateOnly effectiveFromUtc, DateOnly effectiveUntilUtc,
        OnCallChainSeed primary, OnCallChainSeed backup, OnCallChainSeed supervisor, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/oncall/rotations",
            new
            {
                chairId,
                shiftCode,
                effectiveFromUtc,
                effectiveUntilUtc,
                primary = Link(primary),
                backup = Link(backup),
                supervisor = Link(supervisor),
            },
            cancellationToken);

    private static object Link(OnCallChainSeed s) => new
    {
        clinicianSub = s.ClinicianSub,
        displayName = s.DisplayName,
        channels = new[] { new { channel = s.Channel, address = s.Address } },
    };

    /// <inheritdoc />
    public Task CreateEscalationPolicyAsync(string name, int criticalPrimarySeconds, int criticalBackupSeconds,
        int warningPrimarySeconds, int warningBackupSeconds, int informationalPrimarySeconds, bool quietHoursSuppressNonCritical, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/oncall/policies",
            new
            {
                name,
                criticalPrimaryWindowSeconds = criticalPrimarySeconds,
                criticalBackupWindowSeconds = criticalBackupSeconds,
                warningPrimaryWindowSeconds = warningPrimarySeconds,
                warningBackupWindowSeconds = warningBackupSeconds,
                informationalPrimaryWindowSeconds = informationalPrimarySeconds,
                quietHoursSuppressNonCritical,
            },
            cancellationToken);

    /// <inheritdoc />
    public Task CreateInventoryItemAsync(string medicationCodeSystem, string medicationCode, string medicationDisplay,
        string lotNumber, DateTime expiryUtc, int initialOnHandUnits, int threshold, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1.0/inventory",
            new { medicationCodeSystem, medicationCode, medicationDisplay, lotNumber, expiryUtc, initialOnHandUnits, threshold },
            cancellationToken);

    /// <inheritdoc />
    public Task IngestIvPumpTelemetryAsync(string vendor, Guid sessionId, Guid chairId, object payload, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client,
            $"api/v1.0/iv-pumps/telemetry?vendor={vendor}&sessionId={sessionId}&chairId={chairId}",
            payload, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasOnCallRotationsAsync(CancellationToken cancellationToken)
    {
        var rows = await HttpJson.GetAsync<List<System.Text.Json.JsonElement>>(
            _client, "api/v1.0/oncall/rotations", cancellationToken).ConfigureAwait(false);
        return (rows?.Count ?? 0) > 0;
    }
}

/// <summary>Typed SmartConnect client (admin API re-mounted under /api/v1/admin, reached via the SmartConnect BFF).</summary>
public sealed class SmartConnectClient : ISmartConnectClient
{
    private readonly HttpClient _client;

    /// <summary>Creates the client.</summary>
    public SmartConnectClient(HttpClient client) => _client = client;

    /// <inheritdoc />
    public async Task<bool> HasFlowsAsync(CancellationToken cancellationToken)
    {
        var rows = await HttpJson.GetAsync<List<System.Text.Json.JsonElement>>(
            _client, "api/v1/admin/flows", cancellationToken).ConfigureAwait(false);
        return (rows?.Count ?? 0) > 0;
    }

    /// <inheritdoc />
    public Task CreateFlowAsync(string name, string description, string dataType, bool started, CancellationToken cancellationToken) =>
        HttpJson.PostAsync(_client, "api/v1/admin/flows",
            new
            {
                id = Guid.NewGuid(),
                name,
                runtimeState = started ? 1 : 0, // 0 = Stopped, 1 = Started
                pipeline = new
                {
                    routeFilters = Array.Empty<object>(),
                    attachmentHandler = (object?)null,
                    sourceTransformStages = Array.Empty<object>(),
                    outboundRoutesSequential = false,
                    outboundRoutes = new[]
                    {
                        new
                        {
                            outboundAdapterKind = "pass-through",
                            outboundParametersJson = (string?)null,
                            maxAttempts = 1,
                            transformStages = Array.Empty<object>(),
                            responseTransformStages = Array.Empty<object>(),
                            reattachAttachments = false,
                        },
                    },
                    scripts = (object?)null,
                    linkedLibraryIds = Array.Empty<string>(),
                    inboundSubscriptions = Array.Empty<object>(),
                },
                tags = new[] { "demo" },
                groupId = (string?)null,
                description,
                dataTypes = new[] { dataType },
                dependencies = Array.Empty<string>(),
                attachments = Array.Empty<object>(),
            },
            cancellationToken);
}
