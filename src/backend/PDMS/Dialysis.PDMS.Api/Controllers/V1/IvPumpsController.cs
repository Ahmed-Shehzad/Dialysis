using Asp.Versioning;
using Dialysis.PDMS.Core.Persistence;
using Dialysis.PDMS.Medications.Domain;
using Dialysis.PDMS.Medications.IvPumps;
using Microsoft.AspNetCore.Mvc;

namespace Dialysis.PDMS.Api.Controllers.V1;

/// <summary>
/// Vendor-agnostic IV-pump telemetry inbound. Vendor edge agents POST raw payloads at
/// <c>/iv-pumps/telemetry?vendor=bd-alaris</c> (or baxter-sigma, plum-360, pcd04); the
/// driver registry dispatches to the matching <see cref="IIvPumpDriver"/> which
/// normalises the payload into a unified <see cref="IvPumpReading"/>. The reading is
/// then applied to the matching <see cref="IvPumpInfusion"/> aggregate — created lazily
/// on the first Start reading and updated on subsequent Progress / Pause / Resume /
/// Alarm / Complete readings.
///
/// Operators read back per-session infusion history via
/// <c>GET /sessions/{id}/iv-pumps/infusions</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/iv-pumps")]
public sealed class IvPumpsController(
    IEnumerable<IIvPumpDriver> drivers,
    IPdmsRepository<IvPumpInfusion, Guid> infusions) : ControllerBase
{
    private readonly Dictionary<string, IIvPumpDriver> _byVendor =
        drivers.ToDictionary(d => d.VendorCode, d => d, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Inbound telemetry — vendor edge agents POST here. The body is the raw vendor
    /// payload; the <c>?vendor=</c> query selects the driver. Returns 202 Accepted plus
    /// the parsed reading so the caller can correlate.
    /// </summary>
    [HttpPost("telemetry")]
    [Consumes("application/json", "application/octet-stream", "text/plain")]
    [ProducesResponseType(typeof(IvPumpReadingDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IngestTelemetryAsync(
        [FromQuery] string vendor,
        [FromQuery] Guid sessionId,
        [FromQuery] Guid chairId,
        CancellationToken cancellationToken)
    {
        if (!_byVendor.TryGetValue(vendor, out var driver))
            return BadRequest($"No driver registered for vendor '{vendor}'.");

        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        var payload = ms.ToArray();

        IvPumpReading reading;
        try
        {
            reading = await driver.ParseAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        catch (FormatException ex)
        {
            return BadRequest(ex.Message);
        }

        var infusion = await FindActiveInfusionAsync(sessionId, reading.PumpDeviceId, cancellationToken)
            .ConfigureAwait(false);

        switch (reading.Kind)
        {
            case IvPumpReadingKind.Start when infusion is null:
                {
                    var fresh = new IvPumpInfusion(
                        id: Guid.CreateVersion7(),
                        sessionId: sessionId,
                        chairId: chairId,
                        pumpDeviceId: reading.PumpDeviceId,
                        vendorCode: reading.VendorCode,
                        medication: reading.MedicationCode is null ? null
                            : new MedicationCoding(reading.MedicationCodeSystem!, reading.MedicationCode, reading.MedicationCode),
                        programmedRateMlPerHour: reading.ProgrammedRateMlPerHour ?? 0m,
                        programmedVolumeMl: reading.ProgrammedVolumeMl ?? 0m,
                        startedAtUtc: reading.CapturedAtUtc);
                    await infusions.AddAsync(fresh, cancellationToken).ConfigureAwait(false);
                    break;
                }
            case IvPumpReadingKind.Progress when infusion is not null:
                infusion.RecordReading(reading.ActualRateMlPerHour ?? 0m, reading.InfusedVolumeMl ?? 0m);
                infusions.Update(infusion);
                break;
            case IvPumpReadingKind.Pause when infusion is not null:
                infusion.Pause();
                infusions.Update(infusion);
                break;
            case IvPumpReadingKind.Resume when infusion is not null:
                infusion.Resume();
                infusions.Update(infusion);
                break;
            case IvPumpReadingKind.Alarm when infusion is not null:
                infusion.MarkAlarm(
                    alarmCode: reading.AlarmCode ?? "UNKNOWN",
                    alarmText: reading.AlarmText ?? "Pump raised an alarm.",
                    severity: reading.AlarmSeverity ?? Dialysis.PDMS.Medications.Contracts.IvPumpAlarmSeverity.Warning,
                    raisedAtUtc: reading.CapturedAtUtc);
                infusions.Update(infusion);
                break;
            case IvPumpReadingKind.Complete when infusion is not null:
                infusion.Complete(reading.InfusedVolumeMl ?? 0m, reading.CapturedAtUtc);
                infusions.Update(infusion);
                break;
        }

        return Accepted(new IvPumpReadingDto(
            VendorCode: reading.VendorCode,
            PumpDeviceId: reading.PumpDeviceId,
            Kind: reading.Kind.ToString(),
            CapturedAtUtc: reading.CapturedAtUtc));
    }

    /// <summary>Lists every infusion attached to a session.</summary>
    [HttpGet("/api/v{version:apiVersion}/sessions/{sessionId:guid}/iv-pumps/infusions")]
    [ProducesResponseType(typeof(IReadOnlyList<InfusionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListInfusionsForSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var all = await infusions.ListAsync(null, cancellationToken).ConfigureAwait(false);
        var rows = all.Where(i => i.SessionId == sessionId).Select(InfusionDto.From).ToArray();
        return Ok(rows);
    }

    private async Task<IvPumpInfusion?> FindActiveInfusionAsync(Guid sessionId, string pumpDeviceId, CancellationToken cancellationToken)
    {
        var all = await infusions.ListAsync(null, cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(i =>
            i.SessionId == sessionId
            && string.Equals(i.PumpDeviceId, pumpDeviceId, StringComparison.OrdinalIgnoreCase)
            && i.Status != IvPumpStatus.Completed);
    }
}

public sealed record IvPumpReadingDto(string VendorCode, string PumpDeviceId, string Kind, DateTime CapturedAtUtc);

public sealed record InfusionDto(
    Guid InfusionId,
    Guid SessionId,
    string PumpDeviceId,
    string VendorCode,
    string Status,
    decimal ProgrammedRateMlPerHour,
    decimal ActualRateMlPerHour,
    decimal ProgrammedVolumeMl,
    decimal InfusedVolumeMl,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string? MedicationCodeSystem,
    string? MedicationCode)
{
    public static InfusionDto From(IvPumpInfusion i) => new(
        InfusionId: i.Id,
        SessionId: i.SessionId,
        PumpDeviceId: i.PumpDeviceId,
        VendorCode: i.VendorCode,
        Status: i.Status.ToString(),
        ProgrammedRateMlPerHour: i.ProgrammedRateMlPerHour,
        ActualRateMlPerHour: i.ActualRateMlPerHour,
        ProgrammedVolumeMl: i.ProgrammedVolumeMl,
        InfusedVolumeMl: i.InfusedVolumeMl,
        StartedAtUtc: i.StartedAtUtc,
        EndedAtUtc: i.EndedAtUtc,
        MedicationCodeSystem: i.Medication?.CodeSystem,
        MedicationCode: i.Medication?.Code);
}
