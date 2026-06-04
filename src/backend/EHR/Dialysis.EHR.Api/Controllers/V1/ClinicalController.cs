using Asp.Versioning;
using Dialysis.CQRS;
using Dialysis.EHR.ClinicalNotes.Features.DraftClinicalNote;
using Dialysis.EHR.ClinicalNotes.Features.OrderLabTest;
using Dialysis.EHR.ClinicalNotes.Features.SignClinicalNote;
using Dialysis.EHR.ClinicalNotes.Features.StartEncounter;
using Dialysis.EHR.Registration.Features.RegisterPatient;
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
    public async Task<IActionResult> RegisterPatientAsync(
        [FromBody] RegisterPatientRequest body,
        CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<RegisterPatientCommand, Guid>(
            new RegisterPatientCommand(
                body.MedicalRecordNumber,
                body.FamilyName,
                body.GivenName,
                body.MiddleName,
                body.DateOfBirth,
                body.SexAtBirthCode,
                body.PreferredLanguageCode),
            cancellationToken).ConfigureAwait(false);
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
    public async Task<IActionResult> OrderLabTestAsync(
        [FromBody] OrderLabTestRequest body,
        CancellationToken cancellationToken)
    {
        var id = await _gateway.SendCommandAsync<OrderLabTestCommand, Guid>(
            new OrderLabTestCommand(
                body.PatientId,
                body.EncounterId,
                body.OrderingProviderId,
                body.LabFacilityCode,
                body.LoincPanelCodes),
            cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/clinical/lab-orders/{id}", new { id });
    }

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
        public void Deconstruct(out Guid PatientId, out Guid EncounterId, out Guid OrderingProviderId, out string LabFacilityCode, out IReadOnlyList<string> LoincPanelCodes)
        {
            PatientId = this.PatientId;
            EncounterId = this.EncounterId;
            OrderingProviderId = this.OrderingProviderId;
            LabFacilityCode = this.LabFacilityCode;
            LoincPanelCodes = this.LoincPanelCodes;
        }
    }
}
