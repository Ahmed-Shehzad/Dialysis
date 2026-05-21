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
public sealed class ClinicalController(ICqrsGateway gateway) : ControllerBase
{
    [HttpPost("patients")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterPatientAsync(
        [FromBody] RegisterPatientRequest body,
        CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<RegisterPatientCommand, Guid>(
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
        var id = await gateway.SendCommandAsync<StartEncounterCommand, Guid>(
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
        await gateway.SendCommandAsync<SignClinicalNoteCommand, Unit>(
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
        var id = await gateway.SendCommandAsync<DraftClinicalNoteCommand, Guid>(
            body, cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/clinical/notes/{id}", new { id });
    }

    [HttpPost("lab-orders")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> OrderLabTestAsync(
        [FromBody] OrderLabTestRequest body,
        CancellationToken cancellationToken)
    {
        var id = await gateway.SendCommandAsync<OrderLabTestCommand, Guid>(
            new OrderLabTestCommand(
                body.PatientId,
                body.EncounterId,
                body.OrderingProviderId,
                body.LabFacilityCode,
                body.LoincPanelCodes),
            cancellationToken).ConfigureAwait(false);
        return Created($"/api/v1.0/clinical/lab-orders/{id}", new { id });
    }

    public sealed record RegisterPatientRequest(
        string MedicalRecordNumber,
        string FamilyName,
        string GivenName,
        string? MiddleName,
        DateOnly DateOfBirth,
        string? SexAtBirthCode,
        string? PreferredLanguageCode);

    public sealed record StartEncounterRequest(
        Guid PatientId,
        Guid ProviderId,
        string EncounterClassCode,
        Guid? AppointmentId);

    public sealed record SignNoteRequest(Guid SigningProviderId);

    public sealed record OrderLabTestRequest(
        Guid PatientId,
        Guid EncounterId,
        Guid OrderingProviderId,
        string LabFacilityCode,
        IReadOnlyList<string> LoincPanelCodes);
}
