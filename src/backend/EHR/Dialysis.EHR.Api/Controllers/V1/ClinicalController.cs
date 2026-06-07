using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.ClinicalNotes.Features.ClinicalDecisionSupport;
using Dialysis.EHR.ClinicalNotes.Features.DraftClinicalNote;
using Dialysis.EHR.ClinicalNotes.Features.ListImagingOrdersForPatient;
using Dialysis.EHR.ClinicalNotes.Features.ListReferralsForPatient;
using Dialysis.EHR.ClinicalNotes.Features.OrderImagingStudy;
using Dialysis.EHR.ClinicalNotes.Features.RequestReferral;
using Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;
using Dialysis.EHR.ClinicalNotes.Features.OrderPrescription;
using Dialysis.EHR.ClinicalNotes.Features.QualityMeasures;
using Dialysis.EHR.ClinicalNotes.Features.ReviewImagingAiFinding;
using Dialysis.EHR.ClinicalNotes.Features.SignClinicalNote;
using Dialysis.EHR.ClinicalNotes.Features.StartEncounter;
using Dialysis.EHR.ClinicalNotes.SafetyChecks;
using Dialysis.EHR.Registration.Features.RegisterPatient;
using Dialysis.DomainDrivenDesign.Exceptions;
using Dialysis.Module.Contracts.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.EHR.Api.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/clinical")]
public sealed class ClinicalController : ControllerBase
{
    private readonly ICqrsGateway _gateway;
    public ClinicalController(ICqrsGateway gateway) => _gateway = gateway;
    [HttpPost("patients")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterPatientAsync(
        [FromBody] RegisterPatientRequest body,
        CancellationToken cancellationToken)
    {
        Guid id;
        try
        {
            id = await _gateway.SendCommandAsync<RegisterPatientCommand, Guid>(
                new RegisterPatientCommand(
                    body.MedicalRecordNumber,
                    body.FamilyName,
                    body.GivenName,
                    body.MiddleName,
                    body.DateOfBirth,
                    body.SexAtBirthCode,
                    body.PreferredLanguageCode),
                cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            // e.g. the MRN is already in use — a conflict with existing state, not a server fault.
            return Conflict(new { error = ex.Message });
        }
        return Created($"/api/v1.0/patients/{id}", new { id });
    }

    [HttpPost("encounters")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> StartEncounterAsync(
        [FromBody] StartEncounterRequest body,
        CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<StartEncounterCommand, Guid>(
            new StartEncounterCommand(body.PatientId, body.ProviderId, body.EncounterClassCode, body.AppointmentId),
            cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/clinical/encounters/{id}", new { id });
    }

    [HttpPost("notes/{noteId:guid}/sign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SignNoteAsync(
        Guid noteId,
        [FromBody] SignNoteRequest body,
        CancellationToken cancellationToken)
    {
        await _gateway.SendCommandAsync<SignClinicalNoteCommand, Unit>(
            new SignClinicalNoteCommand(noteId, body.SigningProviderId),
            cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost("notes/draft")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> DraftNoteAsync(
        [FromBody] DraftClinicalNoteCommand body,
        CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<DraftClinicalNoteCommand, Guid>(
            body, cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/clinical/notes/{id}", new { id });
    }

    [HttpPost("lab-orders")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> OrderLabTestAsync(
        [FromBody] OrderLabTestRequest body,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        try
        {
            var result = await _gateway.SendCommandAsync<OrderLabTestCommand, OrderPlacementResult>(
                new OrderLabTestCommand(
                    body.PatientId,
                    body.EncounterId,
                    body.OrderingProviderId,
                    body.LabFacilityCode,
                    body.LoincPanelCodes,
                    AcknowledgeAdvisories: body.AcknowledgeAdvisories,
                    OverrideReason: body.OverrideReason,
                    OverriddenBy: OverriderOf(currentUser)),
                cancellationToken).ConfigureAwait(false);
            return Created(
                $"/api/v1.0/clinical/lab-orders/{result.Id}",
                new { id = result.Id, advisories = result.Advisories.Select(ToAdvisoryDto) });
        }
        catch (ClinicalSafetyBlockedException ex)
        {
            return UnprocessableEntity(new { advisories = ex.Advisories.Select(ToAdvisoryDto) });
        }
    }

    /// <summary>
    /// Issues a prescription. Runs point-of-care safety checks (medication↔allergy, duplicate medication);
    /// a blocking advisory returns <c>422</c> with the advisory list until the prescriber re-submits with
    /// <c>acknowledgeAdvisories=true</c> and an <c>overrideReason</c> (audited on the prescription).
    /// </summary>
    [HttpPost("prescriptions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> OrderPrescriptionAsync(
        [FromBody] OrderPrescriptionRequest body,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        try
        {
            var result = await _gateway.SendCommandAsync<OrderPrescriptionCommand, OrderPlacementResult>(
                new OrderPrescriptionCommand(
                    body.PatientId,
                    body.EncounterId,
                    body.PrescribingProviderId,
                    body.MedicationRxnormCode,
                    body.MedicationDisplay,
                    body.DoseText,
                    body.FrequencyText,
                    body.QuantityDispensed,
                    body.RefillsAuthorized,
                    body.PharmacyNcpdpId,
                    AcknowledgeAdvisories: body.AcknowledgeAdvisories,
                    OverrideReason: body.OverrideReason,
                    OverriddenBy: OverriderOf(currentUser)),
                cancellationToken).ConfigureAwait(false);
            return Created(
                $"/api/v1.0/clinical/prescriptions/{result.Id}",
                new { id = result.Id, advisories = result.Advisories.Select(ToAdvisoryDto) });
        }
        catch (ClinicalSafetyBlockedException ex)
        {
            return UnprocessableEntity(new { advisories = ex.Advisories.Select(ToAdvisoryDto) });
        }
    }

    /// <summary>
    /// Refers / transfers a patient to an external organisation. Fires the HIE CCD push by raising
    /// <c>ReferralRequestedIntegrationEvent</c>.
    /// </summary>
    [HttpPost("referrals")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> RequestReferralAsync(
        [FromBody] RequestReferralRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await _gateway.SendCommandAsync<RequestReferralCommand, Guid>(
            new RequestReferralCommand(
                body.PatientId, body.DestinationPartnerId, body.ReferringProviderId, body.ReferralReason),
            cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/clinical/referrals/{id}", new { id });
    }

    /// <summary>
    /// Lists the patient's open quality / MIPS care gaps for the chart's quality card. Empty unless
    /// measures are configured (<c>Ehr:QualityMeasures</c>).
    /// </summary>
    [HttpGet("patients/{patientId:guid}/quality-gaps")]
    [ProducesResponseType(typeof(IReadOnlyList<QualityGap>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListQualityGapsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var gaps = await _gateway.SendQueryAsync<GetQualityGapsQuery, IReadOnlyList<QualityGap>>(
            new GetQualityGapsQuery(patientId), cancellationToken).ConfigureAwait(false);
        return Ok(gaps);
    }

    /// <summary>
    /// Lists the patient's currently-firing clinical decision-support recommendations for the chart.
    /// Empty unless rules are configured (<c>Ehr:Cds</c>).
    /// </summary>
    [HttpGet("patients/{patientId:guid}/clinical-recommendations")]
    [ProducesResponseType(typeof(IReadOnlyList<CdsRecommendation>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListClinicalRecommendationsAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var recommendations = await _gateway.SendQueryAsync<GetClinicalRecommendationsQuery, IReadOnlyList<CdsRecommendation>>(
            new GetClinicalRecommendationsQuery(patientId), cancellationToken).ConfigureAwait(false);
        return Ok(recommendations);
    }

    /// <summary>Lists a patient's referrals (most-recent first) for the chart's referral history.</summary>
    [HttpGet("patients/{patientId:guid}/referrals")]
    [ProducesResponseType(typeof(IReadOnlyList<ReferralDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListReferralsAsync(
        Guid patientId, [FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var rows = await _gateway.SendQueryAsync<ListReferralsForPatientQuery, IReadOnlyList<ReferralDto>>(
            new ListReferralsForPatientQuery(patientId, take), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    /// <summary>Referral request body.</summary>
    public sealed record RequestReferralRequest(
        Guid PatientId,
        string DestinationPartnerId,
        Guid ReferringProviderId,
        string? ReferralReason);

    private static string OverriderOf(ICurrentUser currentUser) => currentUser.UserId ?? "clinician";

    private static object ToAdvisoryDto(SafetyAdvisory a) => new
    {
        category = a.Category.ToString(),
        severity = a.Severity.ToString(),
        matchedCode = a.MatchedCode,
        matchedDisplay = a.MatchedDisplay,
        orderedConcept = a.OrderedConcept,
        sourceRowId = a.SourceRowId,
        sourceKind = a.SourceKind,
        detail = a.Detail,
    };

    /// <summary>Orders an imaging study; the modality fulfils it and STOWs the study back via DICOM.</summary>
    [HttpPost("imaging-orders")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> OrderImagingStudyAsync(
        [FromBody] OrderImagingStudyRequest body,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var id = await _gateway.SendCommandAsync<OrderImagingStudyCommand, Guid>(
            new OrderImagingStudyCommand(
                body.PatientId,
                body.EncounterId,
                body.OrderingProviderId,
                body.ModalityCode,
                body.BodySiteCode,
                body.ReasonText),
            cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/clinical/imaging-orders/{id}", new { id });
    }

    /// <summary>Lists a patient's imaging orders (most-recent first) for the chart imaging panel.</summary>
    [HttpGet("patients/{patientId:guid}/imaging-orders")]
    [ProducesResponseType(typeof(IReadOnlyList<ImagingOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListImagingOrdersAsync(
        Guid patientId, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var rows = await _gateway.SendQueryAsync<ListImagingOrdersForPatientQuery, IReadOnlyList<ImagingOrderDto>>(
            new ListImagingOrdersForPatientQuery(patientId, take), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    /// <summary>Human-in-the-loop sign-off on an order's advisory AI finding (accept or reject).</summary>
    [HttpPost("imaging-orders/{id:guid}/ai-finding/review")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReviewImagingAiFindingAsync(
        Guid id,
        [FromBody] ReviewImagingAiFindingRequest body,
        [FromServices] Dialysis.Module.Contracts.Authorization.ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var reviewedBy = currentUser.UserId ?? User.Identity?.Name ?? "clinician";
        try
        {
            await _gateway.SendCommandAsync<ReviewImagingAiFindingCommand, Unit>(
                new ReviewImagingAiFindingCommand(id, body.Accepted, reviewedBy), cancellationToken)
                .ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Imaging-order request body.</summary>
    public sealed record OrderImagingStudyRequest(
        Guid PatientId,
        Guid EncounterId,
        Guid OrderingProviderId,
        string ModalityCode,
        string BodySiteCode,
        string? ReasonText);

    /// <summary>AI-finding sign-off request body.</summary>
    public sealed record ReviewImagingAiFindingRequest(bool Accepted);

    public sealed record RegisterPatientRequest
    {
        public RegisterPatientRequest(string MedicalRecordNumber,
            string FamilyName,
            string GivenName,
            string? MiddleName,
            DateOnly DateOfBirth,
            string? SexAtBirthCode,
            string? PreferredLanguageCode)
        {
            this.MedicalRecordNumber = MedicalRecordNumber;
            this.FamilyName = FamilyName;
            this.GivenName = GivenName;
            this.MiddleName = MiddleName;
            this.DateOfBirth = DateOfBirth;
            this.SexAtBirthCode = SexAtBirthCode;
            this.PreferredLanguageCode = PreferredLanguageCode;
        }
        public string MedicalRecordNumber { get; init; }
        public string FamilyName { get; init; }
        public string GivenName { get; init; }
        public string? MiddleName { get; init; }
        public DateOnly DateOfBirth { get; init; }
        public string? SexAtBirthCode { get; init; }
        public string? PreferredLanguageCode { get; init; }
        public void Deconstruct(out string MedicalRecordNumber, out string FamilyName, out string GivenName, out string? MiddleName, out DateOnly DateOfBirth, out string? SexAtBirthCode, out string? PreferredLanguageCode)
        {
            MedicalRecordNumber = this.MedicalRecordNumber;
            FamilyName = this.FamilyName;
            GivenName = this.GivenName;
            MiddleName = this.MiddleName;
            DateOfBirth = this.DateOfBirth;
            SexAtBirthCode = this.SexAtBirthCode;
            PreferredLanguageCode = this.PreferredLanguageCode;
        }
    }

    public sealed record StartEncounterRequest
    {
        public StartEncounterRequest(Guid PatientId,
            Guid ProviderId,
            string EncounterClassCode,
            Guid? AppointmentId)
        {
            this.PatientId = PatientId;
            this.ProviderId = ProviderId;
            this.EncounterClassCode = EncounterClassCode;
            this.AppointmentId = AppointmentId;
        }
        public Guid PatientId { get; init; }
        public Guid ProviderId { get; init; }
        public string EncounterClassCode { get; init; }
        public Guid? AppointmentId { get; init; }
        public void Deconstruct(out Guid PatientId, out Guid ProviderId, out string EncounterClassCode, out Guid? AppointmentId)
        {
            PatientId = this.PatientId;
            ProviderId = this.ProviderId;
            EncounterClassCode = this.EncounterClassCode;
            AppointmentId = this.AppointmentId;
        }
    }

    public sealed record SignNoteRequest
    {
        public SignNoteRequest(Guid SigningProviderId) => this.SigningProviderId = SigningProviderId;
        public Guid SigningProviderId { get; init; }
        public void Deconstruct(out Guid SigningProviderId) => SigningProviderId = this.SigningProviderId;
    }

    public sealed record OrderLabTestRequest
    {
        public OrderLabTestRequest(Guid PatientId,
            Guid EncounterId,
            Guid OrderingProviderId,
            string LabFacilityCode,
            IReadOnlyList<string> LoincPanelCodes)
        {
            this.PatientId = PatientId;
            this.EncounterId = EncounterId;
            this.OrderingProviderId = OrderingProviderId;
            this.LabFacilityCode = LabFacilityCode;
            this.LoincPanelCodes = LoincPanelCodes;
        }
        public Guid PatientId { get; init; }
        public Guid EncounterId { get; init; }
        public Guid OrderingProviderId { get; init; }
        public string LabFacilityCode { get; init; }
        public IReadOnlyList<string> LoincPanelCodes { get; init; }

        /// <summary>When true, blocking safety advisories are overridden (requires <see cref="OverrideReason"/>).</summary>
        public bool AcknowledgeAdvisories { get; init; }

        /// <summary>The ordering provider's reason for overriding a blocking advisory.</summary>
        public string? OverrideReason { get; init; }
        public void Deconstruct(out Guid PatientId, out Guid EncounterId, out Guid OrderingProviderId, out string LabFacilityCode, out IReadOnlyList<string> LoincPanelCodes)
        {
            PatientId = this.PatientId;
            EncounterId = this.EncounterId;
            OrderingProviderId = this.OrderingProviderId;
            LabFacilityCode = this.LabFacilityCode;
            LoincPanelCodes = this.LoincPanelCodes;
        }
    }

    /// <summary>Prescription request body. The override fields are honoured only when a blocking advisory is raised.</summary>
    public sealed record OrderPrescriptionRequest(
        Guid PatientId,
        Guid EncounterId,
        Guid PrescribingProviderId,
        string MedicationRxnormCode,
        string MedicationDisplay,
        string DoseText,
        string FrequencyText,
        int QuantityDispensed,
        int RefillsAuthorized,
        string PharmacyNcpdpId,
        bool AcknowledgeAdvisories = false,
        string? OverrideReason = null);
}
