using Asp.Versioning;
using Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Medications.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

/// <summary>
/// Chairside MAR (medication-administration-record) surface. The SPA's Medications tab
/// inside the live-session view drives every request here:
/// <list type="bullet">
///   <item><c>GET /sessions/{id}/medications</c> — list every administration / decline.</item>
///   <item><c>POST /sessions/{id}/medications</c> — record a positive administration.</item>
///   <item><c>POST /sessions/{id}/medications/{entryId}/decline</c> — record a decline with reason.</item>
/// </list>
///
/// The MAR aggregate is created lazily on the first write — operators don't have to
/// "open" the MAR explicitly; recording an administration on a session that doesn't have
/// a MAR yet creates one with that session's open time.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sessions/{sessionId:guid}/medications")]
public sealed class MedicationsController(
    IPdmsRepository<MedicationAdministrationRecord, Guid> records,
    TimeProvider clock) : ControllerBase
{
    [HttpGet]
    [PhiAccess("pdms.medications.read", PatientIdRouteKey = "sessionId", SessionIdRouteKey = "sessionId")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicationEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var mar = await FindMarForSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (mar is null) return Ok(Array.Empty<MedicationEntryDto>());
        var rows = mar.Entries.Select(MedicationEntryDto.From).ToArray();
        return Ok(rows);
    }

    [HttpPost]
    [PhiAccess("pdms.medications.administer", PatientIdRouteKey = "sessionId", SessionIdRouteKey = "sessionId")]
    [ProducesResponseType(typeof(MedicationEntryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordAdministrationAsync(
        Guid sessionId,
        [FromBody] RecordAdministrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var mar = await GetOrCreateMarAsync(sessionId, request.PatientId, cancellationToken).ConfigureAwait(false);
        var coding = new MedicationCoding(request.CodeSystem, request.Code, request.Display);
        var dose = new Dose(request.DoseQuantity, request.DoseUnit);
        if (!Enum.TryParse<MedicationRoute>(request.Route, ignoreCase: true, out var route))
            return BadRequest($"Unknown medication route '{request.Route}'.");

        var entryId = Guid.CreateVersion7();
        var entry = mar.RecordAdministration(
            entryId: entryId,
            medication: coding,
            dose: dose,
            route: route,
            administeredAtUtc: request.AdministeredAtUtc ?? clock.GetUtcNow().UtcDateTime,
            administeredBySub: request.AdministeredBySub,
            relatedOrderId: request.RelatedOrderId);
        records.Update(mar);
        return CreatedAtAction(nameof(ListAsync), new { sessionId }, MedicationEntryDto.From(entry));
    }

    [HttpPost("{entryId:guid}/decline")]
    [PhiAccess("pdms.medications.decline", PatientIdRouteKey = "sessionId", SessionIdRouteKey = "sessionId")]
    [ProducesResponseType(typeof(MedicationEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RecordDeclineAsync(
        Guid sessionId,
        Guid entryId,
        [FromBody] RecordDeclineRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var mar = await GetOrCreateMarAsync(sessionId, request.PatientId, cancellationToken).ConfigureAwait(false);
        var coding = new MedicationCoding(request.CodeSystem, request.Code, request.Display);
        var dose = new Dose(request.DoseQuantity, request.DoseUnit);
        if (!Enum.TryParse<MedicationRoute>(request.Route, ignoreCase: true, out var route))
            return BadRequest($"Unknown medication route '{request.Route}'.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("Decline reason is required.");

        var entry = mar.RecordDecline(
            entryId: entryId,
            medication: coding,
            dose: dose,
            route: route,
            declinedAtUtc: clock.GetUtcNow().UtcDateTime,
            declinedBySub: request.DeclinedBySub,
            reason: request.Reason,
            relatedOrderId: request.RelatedOrderId);
        records.Update(mar);
        return Ok(MedicationEntryDto.From(entry));
    }

    private async Task<MedicationAdministrationRecord?> FindMarForSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var all = await records.ListAsync(null, cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(m => m.SessionId == sessionId);
    }

    private async Task<MedicationAdministrationRecord> GetOrCreateMarAsync(Guid sessionId, Guid patientId, CancellationToken cancellationToken)
    {
        var existing = await FindMarForSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) return existing;
        var fresh = new MedicationAdministrationRecord(
            id: Guid.CreateVersion7(),
            sessionId: sessionId,
            patientId: patientId,
            openedAtUtc: clock.GetUtcNow().UtcDateTime);
        await records.AddAsync(fresh, cancellationToken).ConfigureAwait(false);
        return fresh;
    }
}

public sealed record RecordAdministrationRequest(
    Guid PatientId,
    string CodeSystem,
    string Code,
    string Display,
    decimal DoseQuantity,
    string DoseUnit,
    string Route,
    string AdministeredBySub,
    DateTime? AdministeredAtUtc = null,
    Guid? RelatedOrderId = null);

public sealed record RecordDeclineRequest(
    Guid PatientId,
    string CodeSystem,
    string Code,
    string Display,
    decimal DoseQuantity,
    string DoseUnit,
    string Route,
    string DeclinedBySub,
    string Reason,
    Guid? RelatedOrderId = null);

public sealed record MedicationEntryDto(
    Guid EntryId,
    string MedicationCodeSystem,
    string MedicationCode,
    string MedicationDisplay,
    decimal DoseQuantity,
    string DoseUnit,
    string Route,
    DateTime OccurredAtUtc,
    string ActorSub,
    bool WasAdministered,
    string? DeclineReason,
    Guid? RelatedOrderId)
{
    public static MedicationEntryDto From(MedicationAdministrationEntry entry) => new(
        EntryId: entry.Id,
        MedicationCodeSystem: entry.Medication.CodeSystem,
        MedicationCode: entry.Medication.Code,
        MedicationDisplay: entry.Medication.DisplayName,
        DoseQuantity: entry.Dose.Quantity,
        DoseUnit: entry.Dose.Unit,
        Route: entry.Route.ToString(),
        OccurredAtUtc: entry.OccurredAtUtc,
        ActorSub: entry.ActorSub,
        WasAdministered: entry.WasAdministered,
        DeclineReason: entry.DeclineReason,
        RelatedOrderId: entry.RelatedOrderId);
}
