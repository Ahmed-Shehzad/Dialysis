using Asp.Versioning;

using Dialysis.Domain.Aggregates;

using Intercessor.Abstractions;

using Microsoft.AspNetCore.Mvc;

namespace Dialysis.Gateway.Features.Meds;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/meds")]
public sealed class MedsController : ControllerBase
{
    private readonly ISender _sender;

    public MedsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Record a medication administration (ESA, iron, heparin, binders). Include X-Tenant-Id header.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MedicationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMedicationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PatientId) || string.IsNullOrWhiteSpace(request.MedicationCode))
            return BadRequest(new { error = "PatientId and MedicationCode are required." });

        var effectiveAt = request.EffectiveAt ?? DateTimeOffset.UtcNow;
        var result = await _sender.SendAsync(
            new CreateMedicationCommand(
                request.PatientId,
                request.MedicationCode,
                request.MedicationDisplay,
                request.DoseQuantity,
                request.DoseUnit,
                request.Route,
                effectiveAt,
                request.SessionId,
                request.ReasonText,
                request.PerformerId),
            cancellationToken);

        if (result.Error is not null)
            return BadRequest(new { error = result.Error });

        if (result.Medication is null)
            return BadRequest();

        return CreatedAtAction(nameof(List), new { patientId = result.Medication.PatientId.Value }, ToDto(result.Medication));
    }

    /// <summary>
    /// List medication administrations for a patient or session. Include X-Tenant-Id header.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MedicationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string patientId,
        [FromQuery] string? sessionId = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            return BadRequest(new { error = "patientId is required." });

        var meds = await _sender.SendAsync(
            new ListMedicationsQuery(patientId, sessionId, limit, offset),
            cancellationToken);

        return Ok(meds.Select(ToDto).ToList());
    }

    private static MedicationDto ToDto(MedicationAdministration m) => new(
        m.Id.ToString(),
        m.PatientId.Value,
        m.SessionId,
        m.MedicationCode,
        m.MedicationDisplay,
        m.DoseQuantity,
        m.DoseUnit,
        m.Route,
        m.EffectiveAt,
        m.Status,
        m.ReasonText);
}

public sealed record CreateMedicationRequest(
    string PatientId,
    string MedicationCode,
    string? MedicationDisplay = null,
    string? DoseQuantity = null,
    string? DoseUnit = null,
    string? Route = null,
    DateTimeOffset? EffectiveAt = null,
    string? SessionId = null,
    string? ReasonText = null,
    string? PerformerId = null);

public sealed record MedicationDto(
    string Id,
    string PatientId,
    string? SessionId,
    string MedicationCode,
    string? MedicationDisplay,
    string? DoseQuantity,
    string? DoseUnit,
    string? Route,
    DateTimeOffset EffectiveAt,
    string? Status,
    string? ReasonText);
