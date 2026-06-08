using Asp.Versioning;
using Dialysis.BuildingBlocks.Fhir.AspNetCore.Audit;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Medications.Domain;
using Dialysis.PDMS.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
public sealed class MedicationsController : ControllerBase
{
    private readonly IPdmsRepository<MedicationAdministrationRecord, Guid> _records;
    private readonly PdmsDbContext _db;
    private readonly TimeProvider _clock;
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
    public MedicationsController(IPdmsRepository<MedicationAdministrationRecord, Guid> records,
        PdmsDbContext db,
        TimeProvider clock)
    {
        _records = records;
        _db = db;
        _clock = clock;
    }
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
        // Validate before opening the MAR so an invalid request doesn't create an empty record.
        if (!Enum.TryParse<MedicationRoute>(request.Route, ignoreCase: true, out var route))
            return BadRequest($"Unknown medication route '{request.Route}'.");

        var mar = await GetOrCreateMarAsync(sessionId, request.PatientId, cancellationToken).ConfigureAwait(false);
        var coding = new MedicationCoding(request.CodeSystem, request.Code, request.Display);
        var dose = new Dose(request.DoseQuantity, request.DoseUnit);

        var entryId = Guid.CreateVersion7();
        var entry = mar.RecordAdministration(
            entryId: entryId,
            medication: coding,
            dose: dose,
            route: route,
            administeredAtUtc: request.AdministeredAtUtc ?? _clock.GetUtcNow().UtcDateTime,
            administeredBySub: request.AdministeredBySub,
            relatedOrderId: request.RelatedOrderId);
        _records.Update(mar);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        // Literal Location URI (not CreatedAtAction): URL-segment API versioning can't resolve the
        // {version} route value for action-link generation, which throws -> 500.
        return Created($"/api/v1.0/sessions/{sessionId}/medications/{entry.Id}", MedicationEntryDto.From(entry));
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
        // Validate before opening the MAR so an invalid request doesn't create an empty record.
        if (!Enum.TryParse<MedicationRoute>(request.Route, ignoreCase: true, out var route))
            return BadRequest($"Unknown medication route '{request.Route}'.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            return BadRequest("Decline reason is required.");

        var mar = await GetOrCreateMarAsync(sessionId, request.PatientId, cancellationToken).ConfigureAwait(false);
        var coding = new MedicationCoding(request.CodeSystem, request.Code, request.Display);
        var dose = new Dose(request.DoseQuantity, request.DoseUnit);

        var entry = mar.RecordDecline(
            entryId: entryId,
            medication: coding,
            dose: dose,
            route: route,
            declinedAtUtc: _clock.GetUtcNow().UtcDateTime,
            declinedBySub: request.DeclinedBySub,
            reason: request.Reason,
            relatedOrderId: request.RelatedOrderId);
        _records.Update(mar);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Ok(MedicationEntryDto.From(entry));
    }

    private async Task<MedicationAdministrationRecord?> FindMarForSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var all = await _records.ListAsync(null, cancellationToken).ConfigureAwait(false);
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
            openedAtUtc: _clock.GetUtcNow().UtcDateTime);
        await _records.AddAsync(fresh, cancellationToken).ConfigureAwait(false);
        try
        {
            // Commit the new MAR on its own so the unique SessionId index arbitrates a concurrent
            // first-write race (two chairside writes opening the same session's MAR at once). The
            // caller then records its entry against the persisted MAR and saves again.
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return fresh;
        }
        catch (DbUpdateException)
        {
            // A concurrent writer opened the MAR for this session first. Detach our loser and use the
            // committed winner so the caller records against it instead of surfacing the violation.
            _db.Entry(fresh).State = EntityState.Detached;
            var winner = await FindMarForSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
            if (winner is null)
                throw;
            return winner;
        }
    }
}

public sealed record RecordAdministrationRequest
{
    public RecordAdministrationRequest(Guid PatientId,
        string CodeSystem,
        string Code,
        string Display,
        decimal DoseQuantity,
        string DoseUnit,
        string Route,
        string AdministeredBySub,
        DateTime? AdministeredAtUtc = null,
        Guid? RelatedOrderId = null)
    {
        this.PatientId = PatientId;
        this.CodeSystem = CodeSystem;
        this.Code = Code;
        this.Display = Display;
        this.DoseQuantity = DoseQuantity;
        this.DoseUnit = DoseUnit;
        this.Route = Route;
        this.AdministeredBySub = AdministeredBySub;
        this.AdministeredAtUtc = AdministeredAtUtc;
        this.RelatedOrderId = RelatedOrderId;
    }
    public Guid PatientId { get; init; }
    public string CodeSystem { get; init; }
    public string Code { get; init; }
    public string Display { get; init; }
    public decimal DoseQuantity { get; init; }
    public string DoseUnit { get; init; }
    public string Route { get; init; }
    public string AdministeredBySub { get; init; }
    public DateTime? AdministeredAtUtc { get; init; }
    public Guid? RelatedOrderId { get; init; }
    public void Deconstruct(out Guid PatientId, out string CodeSystem, out string Code, out string Display, out decimal DoseQuantity, out string DoseUnit, out string Route, out string AdministeredBySub, out DateTime? AdministeredAtUtc, out Guid? RelatedOrderId)
    {
        PatientId = this.PatientId;
        CodeSystem = this.CodeSystem;
        Code = this.Code;
        Display = this.Display;
        DoseQuantity = this.DoseQuantity;
        DoseUnit = this.DoseUnit;
        Route = this.Route;
        AdministeredBySub = this.AdministeredBySub;
        AdministeredAtUtc = this.AdministeredAtUtc;
        RelatedOrderId = this.RelatedOrderId;
    }
}

public sealed record RecordDeclineRequest
{
    public RecordDeclineRequest(Guid PatientId,
        string CodeSystem,
        string Code,
        string Display,
        decimal DoseQuantity,
        string DoseUnit,
        string Route,
        string DeclinedBySub,
        string Reason,
        Guid? RelatedOrderId = null)
    {
        this.PatientId = PatientId;
        this.CodeSystem = CodeSystem;
        this.Code = Code;
        this.Display = Display;
        this.DoseQuantity = DoseQuantity;
        this.DoseUnit = DoseUnit;
        this.Route = Route;
        this.DeclinedBySub = DeclinedBySub;
        this.Reason = Reason;
        this.RelatedOrderId = RelatedOrderId;
    }
    public Guid PatientId { get; init; }
    public string CodeSystem { get; init; }
    public string Code { get; init; }
    public string Display { get; init; }
    public decimal DoseQuantity { get; init; }
    public string DoseUnit { get; init; }
    public string Route { get; init; }
    public string DeclinedBySub { get; init; }
    public string Reason { get; init; }
    public Guid? RelatedOrderId { get; init; }
    public void Deconstruct(out Guid PatientId, out string CodeSystem, out string Code, out string Display, out decimal DoseQuantity, out string DoseUnit, out string Route, out string DeclinedBySub, out string Reason, out Guid? RelatedOrderId)
    {
        PatientId = this.PatientId;
        CodeSystem = this.CodeSystem;
        Code = this.Code;
        Display = this.Display;
        DoseQuantity = this.DoseQuantity;
        DoseUnit = this.DoseUnit;
        Route = this.Route;
        DeclinedBySub = this.DeclinedBySub;
        Reason = this.Reason;
        RelatedOrderId = this.RelatedOrderId;
    }
}

public sealed record MedicationEntryDto
{
    public MedicationEntryDto(Guid EntryId,
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
        this.EntryId = EntryId;
        this.MedicationCodeSystem = MedicationCodeSystem;
        this.MedicationCode = MedicationCode;
        this.MedicationDisplay = MedicationDisplay;
        this.DoseQuantity = DoseQuantity;
        this.DoseUnit = DoseUnit;
        this.Route = Route;
        this.OccurredAtUtc = OccurredAtUtc;
        this.ActorSub = ActorSub;
        this.WasAdministered = WasAdministered;
        this.DeclineReason = DeclineReason;
        this.RelatedOrderId = RelatedOrderId;
    }
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
    public Guid EntryId { get; init; }
    public string MedicationCodeSystem { get; init; }
    public string MedicationCode { get; init; }
    public string MedicationDisplay { get; init; }
    public decimal DoseQuantity { get; init; }
    public string DoseUnit { get; init; }
    public string Route { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public string ActorSub { get; init; }
    public bool WasAdministered { get; init; }
    public string? DeclineReason { get; init; }
    public Guid? RelatedOrderId { get; init; }
    public void Deconstruct(out Guid EntryId, out string MedicationCodeSystem, out string MedicationCode, out string MedicationDisplay, out decimal DoseQuantity, out string DoseUnit, out string Route, out DateTime OccurredAtUtc, out string ActorSub, out bool WasAdministered, out string? DeclineReason, out Guid? RelatedOrderId)
    {
        EntryId = this.EntryId;
        MedicationCodeSystem = this.MedicationCodeSystem;
        MedicationCode = this.MedicationCode;
        MedicationDisplay = this.MedicationDisplay;
        DoseQuantity = this.DoseQuantity;
        DoseUnit = this.DoseUnit;
        Route = this.Route;
        OccurredAtUtc = this.OccurredAtUtc;
        ActorSub = this.ActorSub;
        WasAdministered = this.WasAdministered;
        DeclineReason = this.DeclineReason;
        RelatedOrderId = this.RelatedOrderId;
    }
}
